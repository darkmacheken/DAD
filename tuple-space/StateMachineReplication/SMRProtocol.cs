using System;
using System.Reflection;

using log4net;

using MessageService;

namespace StateMachineReplication {
   
    public class SMRProtocol : IProtocol {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ReplicaState replicaState;

        private MessageServiceClient messageServiceClient;

        private string serverId;


        public void Init(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.serverId = serverId;
            this.messageServiceClient = messageServiceClient;
            this.replicaState = new ReplicaState(this, url, serverId);
        }

        public IResponse ProcessRequest(ISenderInformation info, IMessage message) {
            throw new NotImplementedException();
        }
    }
}
