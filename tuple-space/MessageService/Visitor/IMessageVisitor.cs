using MessageService.Serializable;

namespace MessageService.Visitor {
    public interface IMessageVisitor {
        IResponse VisitAddRequest(AddRequest addRequest);

        IResponse VisitTakeRequest(TakeRequest takeRequest);

        IResponse VisitReadRequest(ReadRequest readRequest);

        IResponse VisitPrepareMessage(PrepareMessage prepareMessage);

        IResponse VisitCommitMessage(CommitMessage commitMessage);

        IResponse VisitHandShakeRequest(HandShakeRequest handShakeRequest);

        IResponse VisitGetAndLock(GetAndLockRequest getAndLockRequest);

        IResponse VisitUnlockRequest(UnlockRequest unlockRequest);
    }
}