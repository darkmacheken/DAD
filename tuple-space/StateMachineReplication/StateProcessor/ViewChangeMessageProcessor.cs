using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;
using Timeout = MessageService.Timeout;

namespace StateMachineReplication.StateProcessor {
    public class ViewChangeMessageProcessor : IMessageSMRVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ViewChangeMessageProcessor));

        private readonly MessageServiceClient messageServiceClient;
        private readonly ReplicaState replicaState;

        private readonly int viewNumber;
        private readonly SortedDictionary<string, Uri> configuration;
        private readonly bool imTheLeader;

        
        private readonly int numberToWait;

        private int messagesDoViewChange;

        public ViewChangeMessageProcessor(
            MessageServiceClient messageServiceClient, 
            ReplicaState replicaState,
            int viewNumber,
            SortedDictionary<string, Uri> configuration) {
            this.messageServiceClient = messageServiceClient;
            this.replicaState = replicaState;
            this.viewNumber = viewNumber;
            this.configuration = configuration;

            this.imTheLeader = this.configuration.Values.ToArray()[0].Equals(this.replicaState.myUrl);

            Uri[] currentConfiguration = this.replicaState.ReplicasUrl.ToArray();
            this.numberToWait = currentConfiguration.Length / 2;

            this.messagesDoViewChange = 0;

            // Start the view change protocol
            Task.Factory.StartNew(this.MulticastStartViewChange);
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

        public IResponse VisitPrepareMessage(PrepareMessage prepareMessage) {
            return this.WaitNormalState(prepareMessage);
        }

        public IResponse VisitCommitMessage(CommitMessage commitMessage) {
            return this.WaitNormalState(commitMessage);
        }

        public IResponse VisitStartViewChange(StartViewChange startViewChange) {
            if (startViewChange.ViewNumber == this.viewNumber &&
                startViewChange.Configuration.Equals(this.configuration)) {
                return new StartViewChangeOk(this.replicaState.ServerId, this.viewNumber, this.configuration);
            }

            return null;
        }

        public IResponse VisitDoViewChange(DoViewChange doViewChange) {
            if (this.imTheLeader &&
                doViewChange.ViewNumber == this.viewNumber &&
                doViewChange.Configuration.Equals(this.configuration) &&
                doViewChange.OldViewNumber == this.replicaState.ViewNumber) {

                Interlocked.Increment(ref this.messagesDoViewChange);
                if (this.messagesDoViewChange >= numberToWait) {
                    // start change
                    Uri[] replicasUrl = this.configuration.Values
                        .Where(url => !url.Equals(this.replicaState.myUrl))
                        .ToArray();

                    IMessage message = new StartChange(this.replicaState.ServerId, this.viewNumber, this.configuration);
                    this.messageServiceClient.RequestMulticast(message, replicasUrl, 0, -1);

                    // Set new configuration
                    this.replicaState.SetNewConfiguration(this.configuration, replicasUrl, this.viewNumber, this.replicaState.ServerId);
                    this.replicaState.ChangeToRecoveryState();
                }
            }
      
            return null;
        }

        public IResponse VisitStartChange(StartChange startChange) {
            // Set new configuration
            Uri[] replicasUrl = this.configuration.Values
                .Where(url => !url.Equals(this.replicaState.myUrl))
                .ToArray();
            this.replicaState.SetNewConfiguration(this.configuration, replicasUrl, this.viewNumber, this.replicaState.ServerId);
            this.replicaState.ChangeToRecoveryState();
            return null;
        }

        public IResponse VisitRecovery(Recovery recovery) {
            while (!(this.replicaState.State is NormalStateMessageProcessor) && 
                   !(this.replicaState.State is RecoveryStateMessageProcessor)) {
                this.replicaState.HandlerStateChanged.WaitOne();
            }
            return recovery.Accept(this.replicaState.State);
        }

        private IResponse WaitNormalState(IMessage message) {
            while (!(this.replicaState.State is NormalStateMessageProcessor)) {
                this.replicaState.HandlerStateChanged.WaitOne();
            }
            return message.Accept(this.replicaState.State);
        }

        private void MulticastStartViewChange() {
            IMessage message = new StartViewChange(this.replicaState.ServerId, this.viewNumber, this.configuration);
            Uri[] currentConfiguration = this.replicaState.ReplicasUrl.ToArray();

            IResponses responses = this.messageServiceClient.RequestMulticast(
                message,
                currentConfiguration,
                currentConfiguration.Length - 1,
                (int)(Timeout.TIMEOUT_HEART_BEAT * 1.2));
            IResponse[] responsesVector = new List<IResponse>(responses.ToArray())
                .Where(response => response != null)
                .ToArray();

            // There was no quorum to accept the view change
            if (responsesVector.Length < this.numberToWait) {
                this.replicaState.ChangeToNormalState();
                return;
            }

            // In case I'm the leader, wait for f DoViewChange
            if (this.imTheLeader) {
                return;
            }

            // Else, send DoViewChange to leader
            Uri leader = this.configuration.Values.ToArray()[0];
            IMessage doViewMessage = new DoViewChange(
                this.replicaState.ServerId, 
                this.viewNumber, 
                this.replicaState.ViewNumber, 
                this.configuration);

            this.messageServiceClient.Request(doViewMessage, leader, -1);
        }

        public override string ToString() {
            return "View Change";
        }
    }
}