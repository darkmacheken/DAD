using MessageService.Serializable;

namespace MessageService.Visitor {
    public interface IMessageVisitor {
        IResponse VisitAddRequest(AddRequest addRequest);

        IResponse VisitTakeRequest(TakeRequest takeRequest);

        IResponse VisitReadRequest(ReadRequest readRequest);
      
        IResponse VisitHandShakeRequest(HandShakeRequest handShakeRequest);
    }

    public interface IMessageSMRVisitor : IMessageVisitor {
        IResponse VisitPrepareMessage(PrepareMessage prepareMessage);

        IResponse VisitCommitMessage(CommitMessage commitMessage);
    }

    public interface IMessageXLVisitor : IMessageVisitor {
        IResponse VisitGetAndLock(GetAndLockRequest getAndLockRequest);

        IResponse VisitUnlockRequest(UnlockRequest unlockRequest);
    }
}