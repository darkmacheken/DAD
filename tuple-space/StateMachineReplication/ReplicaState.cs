using System;
using System.Collections.Generic;

using MessageService;

namespace StateMachineReplication {
    public class ReplicaState {
        public string ServerId { get; }

        private readonly SortedDictionary<string, Uri> configuration;
        private readonly List<Tuple<ISenderInformation, IMessage>> logger;
        private readonly Dictionary<string, IResponse> clientTable;
        private string leader;
        private string state;
        private int viewNumber;
        private int opNumber;
        private int commitNumber;

        public ReplicaState(SMRProtocol protocol, Uri url, string serverId) {
            this.ServerId = serverId;
            this.configuration = new SortedDictionary<string, Uri> { { this.ServerId, url } };
            this.logger = new List<Tuple<ISenderInformation, IMessage>>();
            this.clientTable = new Dictionary<string, IResponse>();
            this.state = "normal";
            this.viewNumber = 0;
            this.opNumber = 0;
            this.commitNumber = 0;
        }
    }
}