using System;
using MessageService.Visitor;

namespace MessageService.Serializable {

    [Serializable]
    public class HeartBeat : IMessage {
        public string ServerId { get; set; }

        public HeartBeat(string serverId) {
            this.ServerId = serverId;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitHeartBeat(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitHeartBeat(this);
        }
    }

    [Serializable]
    public class HeartBeatResponse : IResponse {
        public int ViewNumber { get; set; }

        public HeartBeatResponse(int viewNumber) {
            this.ViewNumber = viewNumber;
        }
    }
}