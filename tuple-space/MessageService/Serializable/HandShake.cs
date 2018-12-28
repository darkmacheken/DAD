using System;
using MessageService.Visitor;

namespace MessageService.Serializable {

    public enum Protocol {
        StateMachineReplication,
        XuLiskov
    }

    [Serializable]
    public class ClientHandShakeRequest : ClientRequest {
        public ClientHandShakeRequest(string clientId) : base(clientId) { }

        public override string ToString() {
            return $"Handshake {{ Client ID: {this.ClientId} }}";
        }

        public override IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitClientHandShakeRequest(this);
        }

        public override IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitClientHandShakeRequest(this);
        }
    }

    [Serializable]
    public class ClientHandShakeResponse : IResponse {
        public Protocol ProtocolUsed { get; set; }
        public int ViewNumber { get; set; }
        public Uri[] ViewConfiguration { get; set; }
        public Uri Leader { get; set; }

        public ClientHandShakeResponse(Protocol protocolUsed, int viewNumber, Uri[] viewConfiguration) {
            this.ProtocolUsed = protocolUsed;
            this.ViewNumber = viewNumber;
            this.ViewConfiguration = viewConfiguration;
        }

        public ClientHandShakeResponse(Protocol protocolUsed, int viewNumber, Uri[] viewConfiguration, Uri leader) {
            this.ProtocolUsed = protocolUsed;
            this.ViewNumber = viewNumber;
            this.ViewConfiguration = viewConfiguration;
            this.Leader = leader;
        }

        public override string ToString() {
            return $"{{ ProtocolUsed: {this.ProtocolUsed}, View Number: {this.ViewNumber}," +
                   $" View Configuration: {this.ViewConfiguration}}}";
        }
    }

    [Serializable]
    public class ServerHandShakeRequest : IMessage {
        public string ServerId { get; set; }
        public Protocol ProtocolUsed { get; set; }

        public ServerHandShakeRequest(string serverId, Protocol protocolUsed) {
            this.ServerId = serverId;
            this.ProtocolUsed = protocolUsed;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitServerHandShakeRequest(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitServerHandShakeRequest(this);
        }
    }

    [Serializable]
    public class ServerHandShakeResponse : IResponse {
        public Uri[] ViewConfiguration { get; set; }

        public ServerHandShakeResponse(Uri[] viewConfiguration) {
            this.ViewConfiguration = viewConfiguration;
        }
    }

    [Serializable]
    public class JoinView : IMessage {
        public string ServerId { get; set; }
        public Uri Url { get; set; }

        public JoinView(string serverId, Uri url) {
            this.ServerId = serverId;
            this.Url = url;
        }

        public IResponse Accept(IMessageVisitor visitor) {
            return visitor.VisitJoinView(this);
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitJoinView(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitJoinView(this);
        }
    }
}