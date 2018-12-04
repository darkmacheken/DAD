using MessageService.Serializable;

namespace MessageService.Visitor {
    public interface IMessageVisitor {
        IResponse VisitAddRequest(AddRequest addRequest);

        IResponse VisitTakeRequest(TakeRequest takeRequest);

        IResponse VisitReadRequest(ReadRequest readRequest);
      
        IResponse VisitClientHandShakeRequest(ClientHandShakeRequest clientHandShakeRequest);

        IResponse VisitServerHandShakeRequest(ServerHandShakeRequest serverHandShakeRequest);

        IResponse VisitJoinView(JoinView joinView);
    }

    public interface IMessageSMRVisitor : IMessageVisitor {
        IResponse VisitPrepareMessage(PrepareMessage prepareMessage);

        IResponse VisitCommitMessage(CommitMessage commitMessage);

        IResponse VisitStartViewChange(StartViewChange startViewChange);

        IResponse VisitDoViewChange(DoViewChange doViewChange);

        IResponse VisitStartChange(StartChange startChange);

        IResponse VisitRecovery(Recovery recovery);
    }

    public interface IMessageXLVisitor : IMessageVisitor {
        IResponse VisitGetAndLock(GetAndLockRequest getAndLockRequest);

        IResponse VisitUnlockRequest(UnlockRequest unlockRequest);
    }
}