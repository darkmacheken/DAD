using System;
using System.Collections.Generic;

using MessageService;
using MessageService.Visitor;

using StateMachineReplication.StateProcess;

namespace StateMachineReplication {
    public class ReplicaState {
        public string ServerId { get; }

        private readonly SortedDictionary<string, Uri> configuration;
        private readonly List<Tuple<ISenderInformation, IMessage>> logger;
        private readonly Dictionary<string, IResponse> clientTable;

        public string Leader { get; set; }
        public IProcessRequestVisitor State { get; set; }
        public int ViewNumber { get; set; }
        public int OpNumber { get; set; }
        public int CommitNumber { get; set;  }

        public ReplicaState(Uri url, string serverId) {
            this.ServerId = serverId;
            this.configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.logger = new List<Tuple<ISenderInformation, IMessage>>();
            this.clientTable = new Dictionary<string, IResponse>();
            this.State = new NormalStateProcessRequest(this);
            this.ViewNumber = 0;
            this.OpNumber = 0;
            this.CommitNumber = 0;
        }
    }
}