using MessageService.Messages;

namespace MessageService.Visitor {
    public interface IProcessRequestVisitor {
        IResponse AcceptAddRequest(AddRequest addRequest);

        IResponse AcceptTakeRequest(TakeRequest takeRequest);

        IResponse AcceptReadRequest(ReadRequest readRequest);

        IResponse AcceptPrepareMessage(PrepareMessage prepareMessage);

        IResponse AcceptCommitMessage(CommitMessage commitMessage);
    }
}