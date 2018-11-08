using System;

using MessageService.Visitor;

namespace MessageService.Messages {
    [Serializable]
    public class PrepareMessage : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public ClientRequest ClientRequest { get; set; }
        public int OpNumber { get; set; }
        public int CommitNumber { get; set; }

        public PrepareMessage(string serverId, int viewNumber, ClientRequest clientRequest, int opNumber, int commitNumber) {
            ServerId = serverId;
            ViewNumber = viewNumber;
            ClientRequest = clientRequest;
            OpNumber = opNumber;
            CommitNumber = commitNumber;
        }

        public IResponse Accept(IProcessRequestVisitor visitor) {
            return visitor.AcceptPrepareMessage(this);
        }

        public override string ToString() {
            return $"{{ Server ID: {this.ServerId}, View Number: {this.ViewNumber}, Client Request: {ClientRequest}, "
                   + $"Op Number: {this.OpNumber}, Commit Number: {this.CommitNumber} }}";
        }
    }

    [Serializable]
    public class PrepareOk : IResponse {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public int OpNumber { get; set; }

        public PrepareOk(string serverId, int viewNumber, int opNumber) {
            ServerId = serverId;
            ViewNumber = viewNumber;
            OpNumber = opNumber;
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
            ServerId = serverId;
            ViewNumber = viewNumber;
            CommitNumber = commitNumber;
        }

        public IResponse Accept(IProcessRequestVisitor visitor) {
            return visitor.AcceptCommitMessage(this);
        }

        public override string ToString() {
            return $"{{ Server ID: {this.ServerId}, View Number: {this.ViewNumber}, Commit Number: {this.CommitNumber} }}";
        }
    }
}