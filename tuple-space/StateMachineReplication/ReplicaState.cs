using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using MessageService;
using MessageService.Messages;
using MessageService.Visitor;

using StateMachineReplication.StateProcess;

namespace StateMachineReplication {
    public class ReplicaState {
        public MessageServiceClient MessageServiceClient { get; }
        public string ServerId { get; }

        public SortedDictionary<string, Uri> Configuration { get; }
        public List<Uri> ReplicasUrls { get; }
        public List<ClientRequest> Logger { get; }
        public Dictionary<string, Tuple<int, ClientResponse>> ClientTable { get; }

        public string Leader { get; set; }
        public IProcessRequestVisitor State { get; set; }

        // Attribute Atomic operations
        private int viewNumber;
        public int ViewNumber { get { return this.viewNumber; } }
        public int IncrementViewNumber() { return Interlocked.Increment(ref this.viewNumber); }

        private int opNumber;
        public int OpNumber { get { return this.opNumber; } }
        public int IncrementOpNumberNumber() { return Interlocked.Increment(ref this.opNumber); }

        private int commitNumber;
        public int CommitNumber { get { return this.commitNumber; } }
        public int IncrementCommitNumber() { return Interlocked.Increment(ref this.commitNumber); }

        public ReplicaState(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.MessageServiceClient = messageServiceClient;
            this.ServerId = serverId;
            this.Configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.ReplicasUrls = new List<Uri> {
                                                  new Uri("tcp://localhost:8081"),
                                                  new Uri("tcp://localhost:8082")
                                              };
            this.Leader = "1";
            this.Logger = new List<ClientRequest>();
            this.ClientTable = new Dictionary<string, Tuple<int, ClientResponse>>();
            this.State = new NormalStateProcessRequest(this, this.MessageServiceClient);
            this.viewNumber = 0;
            this.opNumber = 0;
            this.commitNumber = 0;
        }

        public bool IAmTheLeader() {
            return string.Equals(this.ServerId, this.Leader);
        }
    }
}