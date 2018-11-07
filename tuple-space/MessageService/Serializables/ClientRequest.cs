using System;

using MessageService.Visitor;

namespace MessageService.Messages {
    [Serializable]
    public abstract  class ClientRequest : IMessage {
        public string Tuple { get; set; }

        protected ClientRequest(string tuple) {
            this.Tuple = tuple;
        }

        public abstract void Accept(ProcessRequestVisitor visitor, ISenderInformation info);
    }

    [Serializable]
    public class ReadRequest : ClientRequest {
        public ReadRequest(string tuple) : base(tuple) { }

        public override void Accept(ProcessRequestVisitor visitor, ISenderInformation info) {
            visitor.AcceptReadRequest(this, info);
        }
    }

    [Serializable]
    public class AddRequest : ClientRequest {
        public AddRequest(string tuple) : base(tuple) { }

        public override void Accept(ProcessRequestVisitor visitor, ISenderInformation info) {
            visitor.AcceptAddRequest(this, info);
        }
    }

    [Serializable]
    public class TakeRequest : ClientRequest {
        public TakeRequest(string tuple) : base(tuple) { }

        public override void Accept(ProcessRequestVisitor visitor, ISenderInformation info) {
            visitor.AcceptTakeRequest(this, info);
        }
    }
}