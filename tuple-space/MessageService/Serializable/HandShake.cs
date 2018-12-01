using System;

using MessageService.Visitor;

namespace MessageService.Serializable {

    public enum Protocol {
        StateMachineReplication,
        XuLiskov
    }

    [Serializable]
    public class HandShakeRequest : ClientRequest {
        public HandShakeRequest(string clientId) : base(clientId) { }

        public IResponse Accept(IMessageVisitor visitor) {
            return visitor.VisitHandShakeRequest(this);
        }

        public override string ToString() {
            return $"Handshake {{ Client ID: {this.ClientId} }}";
        }

        public override IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitHandShakeRequest(this);
        }

        public override IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitHandShakeRequest(this);
        }
    }

    [Serializable]
    public class HandShakeResponse : IResponse {
        public Protocol ProtocolUsed { get; set; }
        public int ViewNumber { get; set; }
        public Uri[] ViewConfiguration { get; set; }

        public HandShakeResponse(Protocol protocolUsed, int viewNumber, Uri[] viewConfiguration) {
            this.ProtocolUsed = protocolUsed;
            this.ViewNumber = viewNumber;
            this.ViewConfiguration = viewConfiguration;
        }

        public override string ToString() {
            return $"{{ ProtocolUsed: {this.ProtocolUsed}, View Number: {this.ViewNumber}," +
                   $" View Configuration: {this.ViewConfiguration}}}";
        }
    }
}