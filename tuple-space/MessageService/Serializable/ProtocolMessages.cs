using System;
using System.Collections.Generic;

using MessageService.Visitor;
using TupleSpace;

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

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitPrepareMessage(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            throw new NotImplementedException();
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

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitCommitMessage(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            throw new NotImplementedException();
        }

        public override string ToString() {
            return $"{{ Server ID: {this.ServerId}, View Number: {this.ViewNumber}, Commit Number: {this.CommitNumber} }}";
        }
    }

    [Serializable]
    public class StartViewChange : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public SortedDictionary<string,Uri> Configuration { get; set; }

        public StartViewChange(string serverId, int viewNumber, SortedDictionary<string, Uri> configuration) {
            ServerId = serverId;
            ViewNumber = viewNumber;
            Configuration = configuration;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitStartViewChange(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class StartViewChangeOk : IResponse {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public SortedDictionary<string, Uri> Configuration { get; set; }

        public StartViewChangeOk(string serverId, int viewNumber, SortedDictionary<string, Uri> configuration) {
            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.Configuration = configuration;
        }
    }

    [Serializable]
    public class DoViewChange : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public int OldViewNumber { get; set; }
        public SortedDictionary<string, Uri> Configuration { get; set; }
        public List<ClientRequest> Logger { get; set; }
        public int OpNumber { get; set; }
        public int CommitNumber { get; set; }

        public DoViewChange(string serverId, 
            int viewNumber, 
            int oldViewNumber, 
            SortedDictionary<string, Uri> configuration, 
            List<ClientRequest> logger,
            int opNumber,
            int commitNumber) {

            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.OldViewNumber = oldViewNumber;
            this.Configuration = configuration;
            this.Logger = logger;
            this.OpNumber = opNumber;
            this.CommitNumber = commitNumber;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitDoViewChange(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class StartChange : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public SortedDictionary<string, Uri> Configuration { get; set; }
        public List<ClientRequest> Logger { get; set; }
        public int OpNumber { get; set; }
        public int CommitNumber { get; set; }

        public StartChange(
            string serverId, 
            int viewNumber, 
            SortedDictionary<string, Uri> configuration,
            List<ClientRequest> logger,
            int opNumber,
            int commitNumber) {

            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.Configuration = configuration;
            this.Logger = logger;
            this.OpNumber = opNumber;
            this.CommitNumber = commitNumber;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitStartChange(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Recovery : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public int OpNumber { get; set; }
        public int CommitNumber { get; set; }

        public Recovery(string serverId, int viewNumber, int opNumber, int commitNumber) {
            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.OpNumber = opNumber;
            this.CommitNumber = commitNumber;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            return visitor.VisitRecovery(this);
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class RecoveryResponse : IResponse {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public int OpNumber { get; set; }
        public int CommitNumber { get; set; }
        public List<ClientRequest> SuffixLogger { get; set; }

        public RecoveryResponse(string serverId) {
            this.ServerId = serverId;
            this.ViewNumber = -1;
            this.OpNumber = -1;
            this.CommitNumber = -1;
            this.SuffixLogger = null;
        }

        public RecoveryResponse(string serverId, int viewNumber, int opNumber, int commitNumber, List<ClientRequest> suffixLogger) {
            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.OpNumber = opNumber;
            this.CommitNumber = commitNumber;
            this.SuffixLogger = suffixLogger;
        }
    }

    // XL ----------------------------------------------------------------------------------------------------------------

    [Serializable]
    public class GetAndLockRequest : ClientRequest {
        public GetAndLockRequest(int viewNumber, string clientId, int requestNumber, string tuple) :
            base(viewNumber, clientId, requestNumber, tuple) { }

        
        public override IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitGetAndLock(this);
        }

        public override IResponse Accept(IMessageSMRVisitor visitor) {
            throw new NotImplementedException();
        }

        public override string ToString() {
            return $"{{ {base.ToString()}, {this.ClientId}, {this.RequestNumber}, {this.Tuple} }}";
        }
    }

    [Serializable]
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

    [Serializable]
    public class UnlockRequest : ClientRequest {

        public UnlockRequest(int viewNumber, string clientId, int requestNumber)
            : base(viewNumber, clientId, requestNumber, string.Empty) { }

        public override IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitUnlockRequest(this);
        }

        public override IResponse Accept(IMessageSMRVisitor visitor) {
            throw new NotImplementedException();
        }

        public override string ToString() {
            return $"{{Client ID: {this.ClientId}, RequestNumber: {this.RequestNumber}}}";
        }
    }

    [Serializable]
    public class StartViewChangeXL : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public SortedDictionary<string, Uri> Configuration { get; set; }

        public StartViewChangeXL(string serverId, int viewNumber, SortedDictionary<string, Uri> configuration) {
            ServerId = serverId;
            ViewNumber = viewNumber;
            Configuration = configuration;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            throw new NotSupportedException();
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitStartViewChangeXL(this);
        }
    }

    [Serializable]
    public class StartViewChangeXLOk : IResponse {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public SortedDictionary<string, Uri> Configuration { get; set; }

        public StartViewChangeXLOk(string serverId, int viewNumber, SortedDictionary<string, Uri> configuration) {
            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.Configuration = configuration;
        }
    }

    [Serializable]
    public class DoViewChangeXL : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public int OldViewNumber { get; set; }
        public SortedDictionary<string, Uri> Configuration { get; set; }
        public TupleSpace.TupleSpace TupleSpace { get; set; }
        public Dictionary<string, Tuple<int, ClientResponse>> ClientTable { get; set; }
        public int CommitNumber { get; set; }

        public DoViewChangeXL(
            string serverId,
            int viewNumber,
            int oldViewNumber,
            SortedDictionary<string, Uri> configuration,
            TupleSpace.TupleSpace tupleSpace,
            Dictionary<string, Tuple<int, ClientResponse>> clientTable,
            int commitNumber) {

            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.OldViewNumber = oldViewNumber;
            this.Configuration = configuration;
            this.TupleSpace = tupleSpace;
            this.ClientTable = clientTable;
            this.CommitNumber = commitNumber;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            throw new NotSupportedException();
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitDoViewChangeXL(this);
        }
    }

    [Serializable]
    public class StartChangeXL : IMessage {
        public string ServerId { get; set; }
        public int ViewNumber { get; set; }
        public SortedDictionary<string, Uri> Configuration { get; set; }
        public TupleSpace.TupleSpace TupleSpace { get; set; }
        public Dictionary<string, Tuple<int, ClientResponse>> ClientTable { get; set; }
        public int CommitNumber { get; set; }

        public StartChangeXL(
            string serverId,
            int viewNumber,
            SortedDictionary<string, Uri> configuration,
            TupleSpace.TupleSpace tupleSpace,
            Dictionary<string, Tuple<int, ClientResponse>> clientTable,
            int commitNumber) {

            this.ServerId = serverId;
            this.ViewNumber = viewNumber;
            this.Configuration = configuration;
            this.TupleSpace = tupleSpace;
            this.ClientTable = clientTable;
            this.CommitNumber = commitNumber;
        }

        public IResponse Accept(IMessageSMRVisitor visitor) {
            throw new NotSupportedException();
        }

        public IResponse Accept(IMessageXLVisitor visitor) {
            return visitor.VisitStartChangeXL(this);
        }
    }
}