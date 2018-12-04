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

namespace StateMachineReplication {
    public enum State { INITIALIZATION, NORMAL, RECOVERING, VIEW_CHANGE }

    public class ReplicaState {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ReplicaState));
        public MessageServiceClient MessageServiceClient { get; }

        // SMR protocol state
        public string ServerId { get; }
        public Uri myUrl { get; }

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

        // Wait Handlers
        public EventWaitHandle HandlerStateChanged { get; }
        public EventWaitHandle HandlersCommits { get; }
        public EventWaitHandle HandlersPrepare { get; }
        public ConcurrentDictionary<string, EventWaitHandle> HandlersClient { get; }

        public ReplicaState(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.MessageServiceClient = messageServiceClient;
            this.ServerId = serverId;
            this.myUrl = url;

            this.Configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.ReplicasUrl = new List<Uri> {
                                                  new Uri("tcp://localhost:8081"),
                                                  new Uri("tcp://localhost:8082")
                                              };
            this.Leader = "1";
            this.Logger = new List<ClientRequest>();
            this.ClientTable = new Dictionary<string, Tuple<int, ClientResponse>>();
            this.State = new InitializationStateMessageProcessor(this, this.MessageServiceClient);
            this.ViewNumber = 0;
            this.opNumber = 0;
            this.commitNumber = 0;
            this.ExecutionQueue = new OrderedQueue();
            this.TupleSpace = new TupleSpace.TupleSpace();
            this.requestsExecutor = new RequestsExecutor(this, this.MessageServiceClient);

            // Handlers
            this.HandlerStateChanged = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.HandlersCommits = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.HandlersPrepare = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.HandlersClient = new ConcurrentDictionary<string, EventWaitHandle>();

            // Task that executes the requests.
            Task.Factory.StartNew(() => {
                while (true) {
                    Executor requestToExecute = this.ExecutionQueue.Take();
                    requestToExecute.Execute(this.requestsExecutor);
                }
            });
        }

        public bool IAmTheLeader() {
            return string.Equals(this.ServerId, this.Leader);
        }

        public void SetNewConfiguration(SortedDictionary<string, Uri> configuration, Uri[] replicasUrl, int newViewNumber, string leader) {
            this.Configuration = configuration;
            this.ReplicasUrl = replicasUrl.ToList();
            this.Leader = leader;
            this.ViewNumber = newViewNumber;
            this.UpdateOpNumber();
        }

        public void ChangeToNormalState() {
            this.State = new NormalStateMessageProcessor(this, this.MessageServiceClient);
            this.HandlerStateChanged.Set();
            this.HandlerStateChanged.Reset();
        }

        public void ChangeToViewChange(int newViewNumber, SortedDictionary<string, Uri> configuration) {
            this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, newViewNumber, configuration);
            this.HandlerStateChanged.Set();
            this.HandlerStateChanged.Reset();
        }

        public void ChangeToViewChange(StartChange startChange) {
            this.State = new ViewChangeMessageProcessor(this.MessageServiceClient, this, startChange);
            this.HandlerStateChanged.Set();
            this.HandlerStateChanged.Reset();
        }

        public void ChangeToRecoveryState() {
            this.State = new RecoveryStateMessageProcessor(this, this.MessageServiceClient);
            this.HandlerStateChanged.Set();
            this.HandlerStateChanged.Reset();
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
        }
    }
}