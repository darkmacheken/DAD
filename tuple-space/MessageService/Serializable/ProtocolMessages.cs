using System;
using System.Collections.Generic;

using MessageService.Visitor;

namespace MessageService.Serializable {

    // SMR ----------------------------------------------------------------------------------------------------------------
    [Serializable]
    public class PrepareMessage : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public ClientRequest ClientRequest { get; set; }
        public int OpNumber { get; set; }
        public int CommitNumber { get; set; }

        public PrepareMessage(string serverId, int viewNumber, ClientRequest clientRequest, int opNumber, int commitNumber) {
            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.ClientRequest = clientRequest;
            this.OpNumber = opNumber;
            this.CommitNumber = commitNumber;
        }

        public IResponse Accept(IMessageVisitor visitor) {
            return visitor.VisitPrepareMessage(this);
        }

        public override string ToString() {
            return $"{{ Server ID: {this.ServerId}, View Number: {this.ViewNumber}, Client Request: {this.ClientRequest}, "
                   + $"Op Number: {this.OpNumber}, Commit Number: {this.CommitNumber} }}";
        }
    }

    [Serializable]
    public class PrepareOk : IResponse {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public int OpNumber { get; set; }

        public PrepareOk(string serverId, int viewNumber, int opNumber) {
            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.OpNumber = opNumber;
        }

        public override string ToString() {
            return $"{{ Server ID: {this.ServerId}, View Number: {this.ViewNumber}, Op Number: {this.OpNumber} }}";
        }
    }
    
    [Serializable]
    public class CommitMessage : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public int CommitNumber { get; set; }

        public CommitMessage(string serverId, int viewNumber, int commitNumber) {
            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.CommitNumber = commitNumber;
        }

        public IResponse Accept(IMessageVisitor visitor) {
            return visitor.VisitCommitMessage(this);
        }

        public override string ToString() {
            return $"{{ Server ID: {this.ServerId}, View Number: {this.ViewNumber}, Commit Number: {this.CommitNumber} }}";
        }
    }

    // XL ----------------------------------------------------------------------------------------------------------------

    public class GetAndLockRequest : ClientRequest {
        public GetAndLockRequest(int viewNumber, string clientId, int requestNumber, string tuple) :
            base(viewNumber, clientId, requestNumber, tuple) { }

        public override IResponse Accept(IMessageVisitor visitor) {
            return visitor.VisitGetAndLock(this);
        }

        public override string ToString() {
            return $"{{ {base.ToString()}, {this.ClientId}, {this.RequestNumber}, {this.Tuple} }}";
        }
    }

    public class GetAndLockResponse : ClientResponse {
        public List<string> Tuples { get; set; }

        public GetAndLockResponse(int requestNumber, int viewNumber, List<string> tuples ) : 
            base(requestNumber, viewNumber) {
            this.Tuples = tuples;
        }

        public override string ToString() {
            return $"{{ RequestNumber: {this.RequestNumber}, ViewNumber: {this.ViewNumber}, Tuples: {this.Tuples}}}";
        }
    }

    public class UnlockRequest : ClientRequest {
        public UnlockRequest(int viewNumber, string clientId, int requestNumber)
            : base(viewNumber, clientId, requestNumber, "") { }

        public override IResponse Accept(IMessageVisitor visitor) {
            return visitor.VisitUnlockRequest(this);
        }

        public override string ToString() {
            return $"{{Client ID: {this.ClientId}, RequestNumber: {this.RequestNumber}}}";
        }
    }
}