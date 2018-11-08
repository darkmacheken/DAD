using System;

using log4net;

using MessageService;

namespace StateMachineReplication {
   
    public class SMRProtocol : IProtocol {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SMRProtocol));

        public ReplicaState ReplicaState { get; private set; }

        public void Init(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.ReplicaState = new ReplicaState(messageServiceClient, url, serverId);
        }

        public IResponse ProcessRequest(IMessage message) {
            return message.Accept(this.ReplicaState.State);
        }
    }
}
