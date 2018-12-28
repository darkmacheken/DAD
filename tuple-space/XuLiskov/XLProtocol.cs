using System;

using log4net;

using MessageService;

namespace XuLiskov {
   
    public class XLProtocol : IProtocol {
        private static readonly ILog Log = LogManager.GetLogger(typeof(XLProtocol));

        public ReplicaState ReplicaState { get; private set; }

        public void Init(MessageServiceClient messageServiceClient, Uri url, string serverId) {
            this.ReplicaState = new ReplicaState(messageServiceClient, url, serverId);
        }

        public string Status() {
            string status;
            lock (this.ReplicaState) {
                 status =
                    $"Protocol: Xu-Liskov {Environment.NewLine}" +
                    $"{this.ReplicaState.Status()}";
            }
            return status;
        }

        public bool QueueWhenFrozen() {
            return true;
        }

        public IResponse ProcessRequest(IMessage message) {
            return message.Accept(this.ReplicaState.State);
        }
    }
}
