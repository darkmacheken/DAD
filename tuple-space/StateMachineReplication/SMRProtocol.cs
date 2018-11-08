using System;
using System.Reflection;

using log4net;

using MessageService;

using StateMachineReplication.StateProcess;

namespace StateMachineReplication {
   
    public class SMRProtocol : IProtocol {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(SMRProtocol));

        public ReplicaState ReplicaState { get; private set; }

        public void Init(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.ReplicaState = new ReplicaState(messageServiceClient, url, serverId);
        }

        public IResponse ProcessRequest(IMessage message) {
            return message.Accept(ReplicaState.State);
        }
    }
}
