using System.Security.Principal;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;

namespace StateMachineReplication.StateProcessor {
    public class RecoveryStateMessageProcessor : IMessageSMRVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RecoveryStateMessageProcessor));

        private readonly MessageServiceClient messageServiceClient;
        private readonly ReplicaState replicaState;

        public RecoveryStateMessageProcessor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.messageServiceClient = messageServiceClient;
            this.replicaState = replicaState;

            Log.Info("Changed to Recovery State.");

            // Start the protocol
            Task.Factory.StartNew(this.RecoveryProtocol);
        }

        public IResponse VisitAddRequest(AddRequest addRequest) {
            return this.WaitNormalState(addRequest);
        }

        public IResponse VisitTakeRequest(TakeRequest takeRequest) {
            return this.WaitNormalState(takeRequest);
        }

        public IResponse VisitReadRequest(ReadRequest readRequest) {
            return this.WaitNormalState(readRequest);
        }

        public IResponse VisitClientHandShakeRequest(ClientHandShakeRequest clientHandShakeRequest) {
            return this.WaitNormalState(clientHandShakeRequest);
        }

        public IResponse VisitServerHandShakeRequest(ServerHandShakeRequest serverHandShakeRequest) {
            return this.WaitNormalState(serverHandShakeRequest);
        }

        public IResponse VisitJoinView(JoinView joinView) {
            return this.WaitNormalState(joinView);
        }

        public IResponse VisitPrepareMessage(PrepareMessage prepareMessage) {
            return this.WaitNormalState(prepareMessage);
        }

        public IResponse VisitCommitMessage(CommitMessage commitMessage) {
            return this.WaitNormalState(commitMessage);
        }

        public IResponse VisitStartViewChange(StartViewChange startViewChange) {
            return this.WaitNormalState(startViewChange);
        }

        public IResponse VisitDoViewChange(DoViewChange doViewChange) {
            return this.WaitNormalState(doViewChange);
        }

        public IResponse VisitStartChange(StartChange startChange) {
            return this.WaitNormalState(startChange);
        }

        public IResponse VisitRecovery(Recovery recovery) {
            if (this.replicaState.OpNumber < recovery.OpNumber) {
                return new RecoveryResponse(this.replicaState.ServerId);
            }

            int count = this.replicaState.Logger.Count;

            return new RecoveryResponse(
                this.replicaState.ServerId,
                this.replicaState.ViewNumber,
                this.replicaState.OpNumber,
                this.replicaState.CommitNumber,
                this.replicaState.Logger.GetRange(recovery.OpNumber, count - recovery.OpNumber));
        }

        private IResponse WaitNormalState(IMessage message) {
            while (!(this.replicaState.State is NormalStateMessageProcessor)) {
                this.replicaState.HandlerStateChanged.WaitOne();
            }
            return message.Accept(this.replicaState.State);
        }

        private void RecoveryProtocol() {
            // Multicast recovery message
            IMessage message = new Recovery(
                this.replicaState.ServerId,
                this.replicaState.ViewNumber,
                this.replicaState.OpNumber,
                this.replicaState.CommitNumber);

            IResponses responses = this.messageServiceClient.RequestMulticast(
                message,
                this.replicaState.ReplicasUrl.ToArray(),
                this.replicaState.Configuration.Count / 2,
                -1,
                true);
            Log.Debug($"Recovery Protocol: got {responses.Count()} responses.");

            RecoveryResponse betterResponse = null;
            foreach (IResponse response in responses.ToArray()) {
                RecoveryResponse recoveryResponse = (RecoveryResponse)response;
                if (recoveryResponse.ViewNumber == this.replicaState.ViewNumber) {
                    if (betterResponse == null) {
                        betterResponse = recoveryResponse;
                        continue;
                    }

                    if (recoveryResponse.OpNumber > betterResponse.OpNumber) {
                        betterResponse = recoveryResponse;
                    }
                }
            }

            if (betterResponse != null &&
                betterResponse.OpNumber > this.replicaState.OpNumber) {
                Log.Debug($"Better Response: OpNumber = {betterResponse.OpNumber}, " +
                          $"CommitNumber = {betterResponse.CommitNumber} ({this.replicaState.CommitNumber})");

                this.replicaState.Logger.AddRange(betterResponse.SuffixLogger);
                this.replicaState.UpdateOpNumber();
                Task.Factory.StartNew(() => {
                    for (int i = this.replicaState.CommitNumber; i < this.replicaState.OpNumber; i++) {
                        ClientRequest clientRequest = this.replicaState.Logger[i];
                        Executor clientExecutor = ExecutorFactory.Factory(clientRequest, i + 1);

                        // Add request to queue

                        OrderedQueue.AddRequestToQueue(this.replicaState, clientRequest, clientExecutor);
                    }
                });

            }
            Log.Debug($"Recovery Protocol: Changing to Normal State.");
            this.replicaState.ChangeToNormalState();
        }

        public override string ToString() {
            return "Recovery";
        }
    }
}