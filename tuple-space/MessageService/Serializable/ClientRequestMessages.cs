using System;
using MessageService.Visitor;

namespace MessageService.Serializable {

    [Serializable]
    public abstract class ClientRequest : IMessage {
        public string ClientId { get; set; }
        public string Tuple { get; set; }
        public int RequestNumber { get; set; }
        public int ViewNumber { get; set; }

        protected ClientRequest(string clientId) {
            this.ClientId = clientId;
        }

        protected ClientRequest(string clientId, int requestNumber, string tuple) {
            this.ClientId = clientId;
            this.RequestNumber = requestNumber;
            this.Tuple = tuple;
        }

        protected ClientRequest(int viewNumber, string clientId, int requestNumber, string tuple) {
            this.ClientId = clientId;
            this.RequestNumber = requestNumber;
            this.Tuple = tuple;
            this.ViewNumber = viewNumber;
        }
        
        public override string ToString() {
            return $"{this.Tuple}";
        }

        public abstract IResponse Accept(IMessageSMRVisitor visitor);

        public abstract IResponse Accept(IMessageXLVisitor visitor);
    }

    [Serializable]
    public class ReadRequest : ClientRequest {
        public ReadRequest(string clientId, int requestNumber, string tuple) : base(clientId, requestNumber, tuple) { }

        public override string ToString() {
            return $"{{ read {base.ToString()}, {this.ClientId}, {this.RequestNumber} }}";
        }

        public override IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitReadRequest(this);
        }

        public override IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitReadRequest(this);
        }
    }

    [Serializable]
    public class AddRequest : ClientRequest {
        public AddRequest(string clientId, int requestNumber, string tuple) : base(clientId, requestNumber, tuple) { }

        public override string ToString() {
            return $"{{ add {base.ToString()}, {this.ClientId}, {this.RequestNumber} }}";
        }

        public override IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitAddRequest(this);
        }

        public override IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitAddRequest(this);
        }
    }

    [Serializable]
    public class TakeRequest : ClientRequest {
        public int RequestNumberLock { get; set; }

        public TakeRequest(string clientId, int requestNumber, string tuple) : base(clientId, requestNumber, tuple) { }


        public TakeRequest(string clientId, int requestNumber, int requestNumberLock, string tuple)
            : base(clientId, requestNumber, tuple) {
            this.RequestNumberLock = requestNumberLock;
        }

        public override string ToString() {
            return $"{{ take: {{tuple: {base.ToString()}, ClientId: {this.ClientId}," +
                   $"RequestNumber: {this.RequestNumber}, RequestUnlockNumber: {this.RequestNumberLock} }} }}";
        }

        public override IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitTakeRequest(this);
        }

        public override IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitTakeRequest(this);
        }
    }

    [Serializable]
    public class ClientResponse : IResponse {
        public int RequestNumber { get; set; }
        public int ViewNumber { get; set; }
        public string Result { get; set; }

        public ClientResponse() { }

        public ClientResponse(int requestNumber, int viewNumber) {
            this.RequestNumber = requestNumber;
            this.ViewNumber = viewNumber;
        }

        public ClientResponse(int requestNumber, int viewNumber, string result) {
            this.RequestNumber = requestNumber;
            this.ViewNumber = viewNumber;
            this.Result = result;
        }

    }
}