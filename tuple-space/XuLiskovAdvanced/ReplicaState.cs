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
using XuLiskovAdvanced.StateProcessor;
using Timeout = MessageService.Timeout;

namespace XuLiskovAdvanced {
    public class ReplicaState {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ReplicaState));

        public MessageServiceClient MessageServiceClient { get; }

        public string ServerId { get; }
        public Uri MyUrl { get; }

        public SortedDictionary<string, Uri> Configuration { get; private set; }
        public List<Uri> ReplicasUrl { get; private set; }
        public string Manager { get; set; }

        public Dictionary<string, Tuple<int, ClientResponse>> ClientTable { get; }

        public IMessageXLVisitor State { get; set; }

        // Tuple Space
        public TupleSpace.TupleSpace TupleSpace { get; private set; }

        // Attribute Atomic operations
        public int ViewNumber { get; private set; }
        private int commitNumber;
        public int CommitNumber => this.commitNumber;
        public int IncrementCommitNumber() { return Interlocked.Increment(ref this.commitNumber); }

        // Request XL Executor
        public RequestsExecutor RequestsExecutor { get; }

        // HeartBeats
        public SortedDictionary<string, DateTime> HeartBeats { get; set; }

        // Handlers
        public EventWaitHandle HandlerStateChanged { get; }
        public ConcurrentDictionary<string, EventWaitHandle> HandlersClient { get; }

        public ReplicaState(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.MessageServiceClient = messageServiceClient;

            this.ServerId = serverId;
            this.MyUrl = url;

            this.Configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.ReplicasUrl = new List<Uri>();

            this.ClientTable = new Dictionary<string, Tuple<int, ClientResponse>>();
            this.State = new InitializationStateMessageProcessor(this, this.MessageServiceClient);
            this.ViewNumber = 0;

            this.TupleSpace = new TupleSpace.TupleSpace();

            this.HeartBeats = new SortedDictionary<string, DateTime>();

            this.RequestsExecutor = new RequestsExecutor(this);

            // Handlers
            this.HandlerStateChanged = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.HandlersClient = new ConcurrentDictionary<string, EventWaitHandle>();

            // Task that checks HeartBeats
            Task.Factory.StartNew(() => {
                while (true) {
                    Thread.Sleep(Timeout.TIMEOUT_VIEW_CHANGE);
                    foreach (KeyValuePair<string, DateTime> entry in this.HeartBeats) {
                        if (entry.Value < DateTime.Now.AddMilliseconds(-Timeout.TIMEOUT_HEART_BEAT_XL * 1.1)) {
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
                    Thread.Sleep(Timeout.TIMEOUT_HEART_BEAT_XL);
                    if (!(this.State is NormalStateMessageProcessor)) {
                        continue;
                    }
                    Task.Factory.StartNew(() => {
                        IResponses responses = this.MessageServiceClient.RequestMulticast(
                            new HeartBeat(this.ServerId),
                            this.ReplicasUrl.ToArray(),
                            this.ReplicasUrl.Count,
                            -1,
                            false);
                        IResponse[] filteredResponses = responses.ToArray()
                            .Where(response => ((HeartBeatResponse) response).ViewNumber > ViewNumber)
                            .ToArray();
                        if (filteredResponses.Length > 0) {
                            this.ChangeToInitializationState();
                        }
                    });
                }
            });
        }

        public void SetNewConfiguration(SortedDictionary<string, Uri> configuration, Uri[] replicasUrl, int newViewNumber) {
            this.Configuration = configuration;
            this.ReplicasUrl = replicasUrl.ToList();
            this.Manager = this.Configuration.Keys.ToArray()[0];
            this.ViewNumber = newViewNumber;
        }

        public void SetNewConfiguration(
            SortedDictionary<string, Uri> configuration,
            Uri[] replicasUrl,
            int newViewNumber,
            TupleSpace.TupleSpace tupleSpace,
            Dictionary<string, Tuple<int, ClientResponse>> clientTable,
            int commitNumber) {
            Log.Warn($"Changing configuration: entering view #{newViewNumber}");

            this.Configuration = configuration;
            this.ReplicasUrl = replicasUrl.ToList();
            this.Manager = this.Configuration.Keys.ToArray()[0];
            this.ViewNumber = newViewNumber;
            this.TupleSpace = tupleSpace;
            this.commitNumber = commitNumber;

            // Create HeartBeat dictionary with entries at DateTime.Now
            DateTime now = DateTime.Now;
            this.HeartBeats = new SortedDictionary<string, DateTime>(
                configuration.Where(kvp => kvp.Key != this.ServerId).ToDictionary(kvp => kvp.Key, kvp => now));
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
                if (!(this.State is ViewChangeMessageProcessor) && !(this.State is InitializationStateMessageProcessor)) {
                    this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, newViewNumber, configuration);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToViewChange(StartChangeXL startChange) {
            lock (this.State) {
                if (!(this.State is ViewChangeMessageProcessor)) {
                    this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, startChange);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public void ChangeToViewChange(DoViewChangeXL doViewChange) {
            lock (this.State) {
                if (!(this.State is ViewChangeMessageProcessor)) {
                    this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, doViewChange);
                    this.HandlerStateChanged.Set();
                    this.HandlerStateChanged.Reset();
                }
            }
        }

        public IResponse UpdateHeartBeat(string serverId) {
            if (this.HeartBeats.ContainsKey(serverId)) {
                this.HeartBeats[serverId] = DateTime.Now;
            }
            return new HeartBeatResponse(this.ViewNumber);
        }

        public string Status() {
            StringBuilder status = new StringBuilder();
            status.Append(
                $"Server ID: {this.ServerId} {Environment.NewLine}" +
                $"Manager: {this.Manager} {Environment.NewLine}" +
                $"View Number: {this.ViewNumber} {Environment.NewLine}" +
                $"Commit Number: {this.commitNumber} {Environment.NewLine}" +
                $"{"View Configuration:", 10} {"Server ID", -10} {"URL", -10}  {Environment.NewLine}");

            foreach (KeyValuePair<string, Uri> entry in this.Configuration) {
                status.Append($"{"                   ", 10} {entry.Key, -10} {entry.Value, -10} {Environment.NewLine}");
            }

            status.Append(
                $"----------------------------- TUPLE SPACE LAYER ------------------------------{Environment.NewLine}");
            status.Append(this.TupleSpace.Status());

            return status.ToString();
        }
    }
}