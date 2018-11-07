using System;

using MessageService.Visitor;

namespace MessageService.Messages {
    [Serializable]
    public class ClientInfo : ISenderInformation {
        public string ClientId { get; set; }

        public ClientInfo(string clientId) {
            ClientId = clientId;
        }

        public override string ToString() {
            return $"{{ client id : {this.ClientId} }}";
        }
    }


    [Serializable]
    public abstract class ClientRequest : IMessage {
        public string Tuple { get; set; }

        protected ClientRequest(string tuple) {
            this.Tuple = tuple;
        }

        public abstract IResponse Accept(IProcessRequestVisitor visitor, ISenderInformation info);

        public override string ToString() {
            return $"{this.Tuple}";
        }
    }

    [Serializable]
    public class ReadRequest : ClientRequest {
        public ReadRequest(string tuple) : base(tuple) { }

        public override IResponse Accept(IProcessRequestVisitor visitor, ISenderInformation info) {
            return visitor.AcceptReadRequest(this, info);
        }

        public override string ToString() {
            return $"{{ read {base.ToString()} }}";
        }
    }

    [Serializable]
    public class AddRequest : ClientRequest {
        public AddRequest(string tuple) : base(tuple) { }

        public override IResponse Accept(IProcessRequestVisitor visitor, ISenderInformation info) {
            return visitor.AcceptAddRequest(this, info);
        }

        public override string ToString() {
            return $"{{ add {base.ToString()} }}";
        }
    }

    [Serializable]
    public class TakeRequest : ClientRequest {
        public TakeRequest(string tuple) : base(tuple) { }

        public override IResponse Accept(IProcessRequestVisitor visitor, ISenderInformation info) {
            return visitor.AcceptTakeRequest(this, info);
        }

        public override string ToString() {
            return $"{{ take {base.ToString()} }}";
        }
    }
}