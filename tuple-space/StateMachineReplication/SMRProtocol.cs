using System;
using System.Collections.Generic;
using System.Reflection;

using log4net;

using MessageService;

namespace StateMachineReplication {
    public class ReplicaState {
        private readonly string serverId;
        private readonly SortedDictionary<string, Uri> configuration;
        private readonly List<Tuple<ISenderInformation, IMessage>> logger;
        private readonly Dictionary<string, IResponse> clientTable;
        private IState state;
        private int viewNumber;
        private int opNumber;
        private int commitNumber;

        public ReplicaState(SMRProtocol protocol, Uri url, string serverId) {
            this.serverId = serverId;
            this.configuration = new SortedDictionary<string, Uri> { { this.serverId, url } };
            this.logger = new List<Tuple<ISenderInformation, IMessage>>();
            this.clientTable = new Dictionary<string, IResponse>();
            this.state = new NormalState(protocol);
            this.viewNumber = 0;
            this.opNumber = 0;
            this.commitNumber = 0;
        }
    }

    public class SMRProtocol : IProtocol {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ReplicaState ReplicaState { get; }

        private readonly MessageServiceClient messageServiceClient;

        public SMRProtocol(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.messageServiceClient = messageServiceClient;
            ReplicaState = new ReplicaState(this, url, serverId);
        }

        public IResponse ProcessRequest(ISenderInformation info, IMessage message) {
            throw new System.NotImplementedException();
        }
    }
}
