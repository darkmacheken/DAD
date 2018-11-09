using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;
using StateMachineReplication;

namespace XuLiskov.StateProcessor {
    public class NormalStateMessageProcessor : IMessageVisitor {
        private ReplicaState replicaState;
        private MessageServiceClient messageServiceClient;

        public NormalStateMessageProcessor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.replicaState = replicaState;
            this.messageServiceClient = messageServiceClient;
        }

        public IResponse VisitAddRequest(AddRequest addRequest) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitTakeRequest(TakeRequest takeRequest) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitReadRequest(ReadRequest readRequest) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitPrepareMessage(PrepareMessage prepareMessage) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitCommitMessage(CommitMessage commitMessage) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitHandShakeRequest(HandShakeRequest handShakeRequest) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitGetAndLock(GetAndLockRequest getAndLockRequest) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitUnlockRequest(UnlockRequest unlockRequest) {
            throw new System.NotImplementedException();
        }
    }
}