using System;
using System.Collections.Generic;
using System.Threading;

using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;

using XuLiskov.StateProcessor;

namespace XuLiskov {
    public class ReplicaState {
        public MessageServiceClient MessageServiceClient { get; }
        public string ServerId { get; }

        public SortedDictionary<string, Uri> Configuration { get; }
        public List<Uri> ReplicasUrls { get; }

        public Dictionary<string, Tuple<int, ClientResponse>> ClientTable { get; }

        public IMessageXLVisitor State { get; set; }

        // Tuple Space
        public TupleSpace.TupleSpace TupleSpace { get; }

        // Attribute Atomic operations
        private int viewNumber;
        public int ViewNumber => this.viewNumber;
        public int IncrementViewNumber() { return Interlocked.Increment(ref this.viewNumber); }

        // Request XL Executor
        public RequestsExecutor RequestsExecutor { get; }

        public ReplicaState(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.MessageServiceClient = messageServiceClient;
            this.ServerId = serverId;
            this.Configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.ReplicasUrls = new List<Uri> {
                                                  new Uri("tcp://localhost:8081"),
                                                  new Uri("tcp://localhost:8082")
                                              };

            this.ClientTable = new Dictionary<string, Tuple<int, ClientResponse>>();
            this.State = new NormalStateMessageProcessor(this, this.MessageServiceClient);
            this.viewNumber = 0;

            this.TupleSpace = new TupleSpace.TupleSpace();

            this.RequestsExecutor = new RequestsExecutor(this);
        }
    }
}