using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;

using StateMachineReplication.StateProcessor;
using Timeout = MessageService.Timeout;

namespace StateMachineReplication {

    public class ReplicaState {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ReplicaState));
        public MessageServiceClient MessageServiceClient { get; }

        // SMR protocol state
        public string ServerId { get; }
        public Uri MyUrl { get; }

        public SortedDictionary<string, Uri> Configuration { get; private set; }
        public List<Uri> ReplicasUrl { get; private set; }
        public List<ClientRequest> Logger { get; private set; }
        public OrderedQueue ExecutionQueue { get; }
        public Dictionary<string, Tuple<int, ClientResponse>> ClientTable { get; }

        public string Leader { get; set; }
        public IMessageSMRVisitor State { get; set; }

        // Attribute Atomic operations
        public int ViewNumber { get; private set; }

        private int opNumber;
        public int OpNumber => this.opNumber;
        public int IncrementOpNumberNumber() { return Interlocked.Increment(ref this.opNumber); }

        private int commitNumber;
        public int CommitNumber => this.commitNumber;
        public int IncrementCommitNumber() { return Interlocked.Increment(ref this.commitNumber); }

        // Tuple Space
        public TupleSpace.TupleSpace TupleSpace { get; }

        // Request SMRExecutor
        private readonly RequestsExecutor requestsExecutor;

        // HeartBeats
        public SortedDictionary<string, DateTime> HeartBeats { get; set; }

        // Wait Handlers
        public EventWaitHandle HandlerStateChanged { get; }
        public EventWaitHandle HandlersCommits { get; }
        public EventWaitHandle HandlersPrepare { get; }
        public ConcurrentDictionary<string, EventWaitHandle> HandlersClient { get; }

        public ReplicaState(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.MessageServiceClient = messageServiceClient;
            this.ServerId = serverId;
            this.MyUrl = url;

            this.Configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.ReplicasUrl = new List<Uri>();
            this.Logger = new List<ClientRequest>();
            this.ClientTable = new Dictionary<string, Tuple<int, ClientResponse>>();
            this.State = new InitializationStateMessageProcessor(this, this.MessageServiceClient);
            this.ViewNumber = 0;
            this.opNumber = 0;
            this.commitNumber = 0;
            this.ExecutionQueue = new OrderedQueue(this);
            this.TupleSpace = new TupleSpace.TupleSpace();
            this.requestsExecutor = new RequestsExecutor(this, this.MessageServiceClient);
            this.HeartBeats = new SortedDictionary<string, DateTime>();


            // Handlers
            this.HandlerStateChanged = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.HandlersCommits = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.HandlersPrepare = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.HandlersClient = new ConcurrentDictionary<string, EventWaitHandle>();

            // Task that executes the requests.
            Task.Factory.StartNew(() => {
                while (true) {
                    if (this.ExecutionQueue.TryTake(out Executor requestToExecute, Timeout.TIMEOUT_RECOVERY)) {
                        requestToExecute.Execute(this.requestsExecutor);
                    } else {
                        this.ChangeToRecoveryState();
                    }
                }
            });

            // Task that checks HeartBeats
            Task.Factory.StartNew(() => {
                while (true) {
                    Thread.Sleep(Timeout.TIMEOUT_VIEW_CHANGE);
                    foreach (KeyValuePair<string, DateTime> entry in this.HeartBeats) {
                        if (entry.Value < DateTime.Now.AddMilliseconds(-Timeout.TIMEOUT_HEART_BEAT * 1.1)) {
                            int newViewNumber = this.ViewNumber + 1;
                            SortedDictionary<string, Uri> newConfiguration = new SortedDictionary<string, Uri>(
                                this.Configuration
                                    .Where(kvp => !kvp.Key.Equals(entry.Key))
                                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

                            Log.Debug($"Server {entry.Key} is presumably dead as the HeartBeat timeout expired.");
                            this.ChangeToViewChange(newViewNumber, newConfiguration);
                            break;
                        }
                    }
                }
            });

            // Task that sends the HeartBeats
            Task.Factory.StartNew(() => {
                while (true) {
                    Thread.Sleep(Timeout.TIMEOUT_HEART_BEAT);
                    if (!(this.State is NormalStateMessageProcessor)) {
                        continue;
                    }
                    Task.Factory.StartNew(() => this.MessageServiceClient.RequestMulticast(
                        new HeartBeat(this.ServerId),
                        this.ReplicasUrl.ToArray(),
                        this.ReplicasUrl.Count,
                        -1,
                        false));
                }
            });
        }

        public bool IAmTheLeader() {
            return string.Equals(this.ServerId, this.Leader);
        }
        
        public void SetNewConfiguration(SortedDictionary<string, Uri> configuration, Uri[] replicasUrl, int newViewNumber) {
            this.Configuration = configuration;
            this.ReplicasUrl = replicasUrl.ToList();
            this.Leader = this.Configuration.Keys.ToArray()[0];
            this.ViewNumber = newViewNumber;
            this.UpdateOpNumber();
        }

        public void SetNewConfiguration(
            SortedDictionary<string, Uri> configuration, 
            Uri[] replicasUrl,
            int newViewNumber, 
            List<ClientRequest> logger,
            int opNumber,
            int commitNumber) {
            Log.Warn($"Changing configuration: entering view #{newViewNumber}");

            this.Configuration = configuration;
            this.ReplicasUrl = replicasUrl.ToList();
            this.Leader = this.Configuration.Keys.ToArray()[0];
            this.ViewNumber = newViewNumber;
            this.Logger = logger;
            this.UpdateOpNumber();

            // Create HeartBeat dictionary with entries at DateTime.Now
            DateTime now = DateTime.Now;
            this.HeartBeats = new SortedDictionary<string, DateTime>(
                configuration.Where(kvp => kvp.Key != this.ServerId).ToDictionary(kvp => kvp.Key, kvp => now));

            // Execute all requests until the received commitNumber
            this.ExecuteFromUntil(this.commitNumber, commitNumber);

            // Execute Missing and send Commit Message
            if (this.IAmTheLeader()) {
                this.ExecuteFromUntil(commitNumber, opNumber);
            }
        }

        public void ExecuteFromUntil(int begin, int end) {
            Task.Factory.StartNew(() => {
                for (int i = begin; i < end; i++) {
                    Executor clientExecutor = ExecutorFactory.Factory(this.Logger[i], i + 1);

                    // Add request to queue
                    Log.Debug($"Trying to add request #{opNumber} in the Execution Queue");
                    OrderedQueue.AddRequestToQueue(this, this.Logger[i], clientExecutor);
                }
            });
        }
        
        public void RestartInitializationState() {
            lock (this.State) {
                if (this.State is InitializationStateMessageProcessor) {
                    this.State = new InitializationStateMessageProcessor(this, this.MessageServiceClient);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToInitializationState() {
            lock (this.State) {
                if (!(this.State is InitializationStateMessageProcessor)) {
                    this.State = new InitializationStateMessageProcessor(this, this.MessageServiceClient);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToNormalState() {
            lock (this.State) {
                if (!(this.State is NormalStateMessageProcessor)) {
                    this.State = new NormalStateMessageProcessor(this, this.MessageServiceClient);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToViewChange(int newViewNumber, SortedDictionary<string, Uri> configuration) {
            lock (this.State) {
                if (!(this.State is ViewChangeMessageProcessor)) {
                    this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, newViewNumber, configuration);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToViewChange(StartChange startChange) {
            lock (this.State) {
                if (!(this.State is ViewChangeMessageProcessor)) {
                    this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, startChange);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToViewChange(DoViewChange doViewChange) {
            lock (this.State) {
                if (!(this.State is ViewChangeMessageProcessor)) {
                    this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, doViewChange);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToRecoveryState() {
            lock (this.State) {
                if (this.State is NormalStateMessageProcessor) {
                    this.State = new RecoveryStateMessageProcessor(this, this.MessageServiceClient);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public string Status() {
            StringBuilder status = new StringBuilder();
            status.Append(
                $"Server ID: {this.ServerId} {Environment.NewLine}" +
                $"Leader: {this.Leader} {Environment.NewLine}" +
                $"State: {this.State} {Environment.NewLine}" +
                $"Op Number: {this.opNumber} {Environment.NewLine}" +
                $"Commit Number: {this.commitNumber} {Environment.NewLine}" +
                $"View Number: {this.ViewNumber} {Environment.NewLine}" +
                $"{"View Configuration:",10} {"Server ID",-10} {"URL",-10}  {Environment.NewLine}");

            foreach (KeyValuePair<string, Uri> entry in this.Configuration) {
                status.Append($"{"                   ",10} {entry.Key,-10} {entry.Value,-10} {Environment.NewLine}");
            }

            status.Append(
                $"----------------------------- TUPLE SPACE LAYER ------------------------------{Environment.NewLine}");
            status.Append(this.TupleSpace.Status());

            return status.ToString();
        }

        public void UpdateOpNumber() {
            this.opNumber = this.Logger.Count;
            this.HandlersPrepare.Set();
            this.HandlersPrepare.Reset();
        }

        public void UpdateHeartBeat(string serverId) {
            if (this.HeartBeats.ContainsKey(serverId)) {
                this.HeartBeats[serverId] = DateTime.Now;
            }
        }
    }
}