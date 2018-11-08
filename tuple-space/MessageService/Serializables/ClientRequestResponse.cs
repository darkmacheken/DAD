using System;

using MessageService.Visitor;

namespace MessageService.Messages {
    [Serializable]
    public abstract class ClientRequest : IMessage {
        public string ClientId { get; set; }
        public string Tuple { get; set; }
        public int RequestNumber { get; set; }

        protected ClientRequest(string clientId, int requestNumber, string tuple) {
            this.ClientId = clientId;
            this.RequestNumber = requestNumber;
            this.Tuple = tuple;
        }

        public abstract IResponse Accept(IProcessRequestVisitor visitor);

        public override string ToString() {
            return $"{this.Tuple}";
        }
    }

    [Serializable]
    public class ReadRequest : ClientRequest {
        public ReadRequest(string clientId, int requestNumber, string tuple) : base(clientId, requestNumber, tuple) { }

        public override IResponse Accept(IProcessRequestVisitor visitor) {
            return visitor.AcceptReadRequest(this);
        }

        public override string ToString() {
            return $"{{ read {base.ToString()}, {this.ClientId}, {this.RequestNumber} }}";
        }
    }

    [Serializable]
    public class AddRequest : ClientRequest {
        public AddRequest(string clientId, int requestNumber, string tuple) : base(clientId, requestNumber, tuple) { }

        public override IResponse Accept(IProcessRequestVisitor visitor) {
            return visitor.AcceptAddRequest(this);
        }

        public override string ToString() {
            return $"{{ add {base.ToString()}, {this.ClientId}, {this.RequestNumber} }}";
        }
    }

    [Serializable]
    public class TakeRequest : ClientRequest {
        public TakeRequest(string clientId, int requestNumber, string tuple) : base(clientId, requestNumber, tuple) { }

        public override IResponse Accept(IProcessRequestVisitor visitor) {
            return visitor.AcceptTakeRequest(this);
        }

        public override string ToString() {
            return $"{{ take {base.ToString()}, {this.ClientId}, {this.RequestNumber} }}";
        }
    }

    [Serializable]
    public class ClientResponse : IResponse {
        public int RequestNumber { get; set; }
        public int ViewNumber { get; set; }
        public string Result { get; set; }

        public ClientResponse(int requestNumber, int viewNumber, string result) {
            RequestNumber = requestNumber;
            ViewNumber = viewNumber;
            Result = result;
        }
    }
}