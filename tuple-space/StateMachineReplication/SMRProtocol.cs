using System;
using System.Reflection;

using log4net;

using MessageService;

using StateMachineReplication.StateProcess;

namespace StateMachineReplication {
   
    public class SMRProtocol : IProtocol {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(SMRProtocol));

        public ReplicaState ReplicaState { get; private set; }

        private MessageServiceClient messageServiceClient;
        
        public void Init(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.messageServiceClient = messageServiceClient;
            this.ReplicaState = new ReplicaState(url, serverId);
        }

        public IResponse ProcessRequest(ISenderInformation info, IMessage message) {
            return message.Accept(ReplicaState.State, info);
        }
    }
}
