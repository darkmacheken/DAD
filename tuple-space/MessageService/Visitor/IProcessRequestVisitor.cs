using MessageService.Serializable;

namespace MessageService.Visitor {
    public interface IProcessRequestVisitor {
        IResponse VisitAddRequest(AddRequest addRequest);

        IResponse VisitTakeRequest(TakeRequest takeRequest);

        IResponse VisitReadRequest(ReadRequest readRequest);

        IResponse VisitPrepareMessage(PrepareMessage prepareMessage);

        IResponse VisitCommitMessage(CommitMessage commitMessage);
    }
}