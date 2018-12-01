using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;

using StateMachineReplication.StateProcessor;

namespace StateMachineReplication {
    public class ReplicaState {
        public MessageServiceClient MessageServiceClient { get; }
        public string ServerId { get; }

        public SortedDictionary<string, Uri> Configuration { get; }
        public List<Uri> ReplicasUrl { get; }
        public List<ClientRequest> Logger { get; }
        public OrderedQueue ExecutionQueue { get; }
        public Dictionary<string, Tuple<int, ClientResponse>> ClientTable { get; }

        public string Leader { get; set; }
        public IMessageSMRVisitor State { get; set; }

        // Attribute Atomic operations
        private int viewNumber;
        public int ViewNumber => this.viewNumber;
        public int IncrementViewNumber() { return Interlocked.Increment(ref this.viewNumber); }

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

        // Handlers
        public ConcurrentDictionary<int, AutoResetEvent> HandlersCommits { get; }
        public ConcurrentDictionary<int, AutoResetEvent> HandlersPrepare { get; }

        public ReplicaState(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.MessageServiceClient = messageServiceClient;
            this.ServerId = serverId;
            this.Configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.ReplicasUrl = new List<Uri> {
                                                  new Uri("tcp://localhost:8081"),
                                                  new Uri("tcp://localhost:8082")
                                              };
            this.Leader = "1";
            this.Logger= new List<ClientRequest>();
            this.ClientTable = new Dictionary<string, Tuple<int, ClientResponse>>();
            this.State = new NormalStateMessageProcessor(this, this.MessageServiceClient);
            this.viewNumber = 0;
            this.opNumber = 0;
            this.commitNumber = 0;
            this.ExecutionQueue = new OrderedQueue();
            this.TupleSpace = new TupleSpace.TupleSpace();
            this.requestsExecutor = new RequestsExecutor(this, this.MessageServiceClient);
            this.HandlersCommits = new ConcurrentDictionary<int, AutoResetEvent>();
            this.HandlersPrepare = new ConcurrentDictionary<int, AutoResetEvent>();

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

        public string Status() {
            StringBuilder status = new StringBuilder();
            status.Append(
                $"Server ID: {this.ServerId} {Environment.NewLine}" +
                $"Leader: {this.Leader} {Environment.NewLine}" +
                $"State: {this.State} {Environment.NewLine}" +
                $"Op Number: {this.opNumber} {Environment.NewLine}" +
                $"Commit Number: {this.commitNumber} {Environment.NewLine}" +
                $"View Number: {this.viewNumber} {Environment.NewLine}" +
                $"{"View Configuration:",10} {"Server ID",10} {"URL",10}  {Environment.NewLine}");

            foreach (KeyValuePair<string, Uri> entry in this.Configuration) {
                status.Append($"{"",10} {entry.Key,10} {entry.Value,10}");
            }

            status.Append(
                $"----------------------------- TUPLE SPACE LAYER ------------------------------{Environment.NewLine}");
            status.Append(this.TupleSpace.Status());

            return status.ToString();
        }
    }
}