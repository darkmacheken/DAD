using System;

using log4net;

using MessageService;

namespace StateMachineReplicationAdvanced {
   
    public class SMRAProtocol : IProtocol {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SMRAProtocol));

        public ReplicaState ReplicaState { get; private set; }

        public void Init(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.ReplicaState = new ReplicaState(messageServiceClient, url, serverId);
        }

        public string Status() {
            string status;
            lock (this.ReplicaState) {
                status =
                    $"Protocol: State Machine Replication {Environment.NewLine}" +
                    $"{this.ReplicaState.Status()}";
            }
            return status;
        }

        public bool QueueWhenFrozen() {
            return false;
        }

        public IResponse ProcessRequest(IMessage message) {
            return message.Accept(this.ReplicaState.State);
        }
    }
}
