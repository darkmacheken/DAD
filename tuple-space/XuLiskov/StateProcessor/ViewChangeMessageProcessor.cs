using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;
using StateMachineReplication.Utils;
using Timeout = MessageService.Timeout;

namespace XuLiskov.StateProcessor {
    public class ViewChangeMessageProcessor : IMessageXLVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ViewChangeMessageProcessor));

        private readonly MessageServiceClient messageServiceClient;
        private readonly ReplicaState replicaState;

        private readonly int viewNumber;
        private readonly SortedDictionary<string, Uri> configuration;
        private readonly bool imTheManager;


        private readonly int numberToWait;
        private int messagesDoViewChange;

        private DoViewChangeXL bestDoViewChange;

        public ViewChangeMessageProcessor(
            MessageServiceClient messageServiceClient,
            ReplicaState replicaState,
            int viewNumber,
            SortedDictionary<string, Uri> configuration) {
            this.messageServiceClient = messageServiceClient;
            this.replicaState = replicaState;

            this.viewNumber = viewNumber;
            this.configuration = configuration;

            this.imTheManager = this.configuration.Values.ToArray()[0].Equals(this.replicaState.MyUrl);
            this.numberToWait = this.replicaState.Configuration.Count / 2;
            this.messagesDoViewChange = 0;

            this.bestDoViewChange = new DoViewChangeXL(
                this.replicaState.ServerId,
                this.viewNumber,
                this.replicaState.ViewNumber,
                this.configuration,
                this.replicaState.TupleSpace,
                this.replicaState.ClientTable,
                this.replicaState.CommitNumber);

            Log.Info("Changed to View Change State.");

            // Start the view change protocol
            Task.Factory.StartNew(this.MulticastStartViewChange);

            // Stay in this state for a timeout
            Task.Factory.StartNew(this.StartTimeout);
        }

        public ViewChangeMessageProcessor(
            MessageServiceClient messageServiceClient,
            ReplicaState replicaState,
            StartChangeXL startChange) {
            this.messageServiceClient = messageServiceClient;
            this.replicaState = replicaState;

            this.viewNumber = startChange.ViewNumber;
            this.configuration = startChange.Configuration;

            this.imTheManager = startChange.Configuration.Values.ToArray()[0].Equals(this.replicaState.MyUrl);
            this.numberToWait = (startChange.Configuration.Count - 1) / 2;
            this.messagesDoViewChange = 0;

            this.bestDoViewChange = new DoViewChangeXL(
                this.replicaState.ServerId,
                this.viewNumber,
                this.replicaState.ViewNumber,
                this.configuration,
                this.replicaState.TupleSpace,
                this.replicaState.ClientTable,
                this.replicaState.CommitNumber);

            Log.Info("Changed to View Change State.");

            // Stay in this state for a timeout
            Task.Factory.StartNew(this.StartTimeout);
        }

        public ViewChangeMessageProcessor(
            MessageServiceClient messageServiceClient,
            ReplicaState replicaState,
            DoViewChangeXL doViewChange) {
            this.messageServiceClient = messageServiceClient;
            this.replicaState = replicaState;

            this.viewNumber = doViewChange.ViewNumber;
            this.configuration = doViewChange.Configuration;

            this.imTheManager = doViewChange.Configuration.Values.ToArray()[0].Equals(this.replicaState.MyUrl);
            this.numberToWait = (doViewChange.Configuration.Count - 1) / 2;
            this.messagesDoViewChange = 0;

            this.bestDoViewChange = new DoViewChangeXL(
                this.replicaState.ServerId,
                this.viewNumber,
                this.replicaState.ViewNumber,
                this.configuration,
                this.replicaState.TupleSpace,
                this.replicaState.ClientTable,
                this.replicaState.CommitNumber);

            Log.Info("Changed to View Change State.");

            // Stay in this state for a timeout
            Task.Factory.StartNew(this.StartTimeout);
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

        public IResponse VisitGetAndLock(GetAndLockRequest getAndLockRequest) {
            return this.WaitNormalState(getAndLockRequest);
        }

        public IResponse VisitUnlockRequest(UnlockRequest unlockRequest) {
            return this.WaitNormalState(unlockRequest);
        }

        public IResponse VisitHeartBeat(HeartBeat heartBeat) {
            return this.replicaState.UpdateHeartBeat(heartBeat.ServerId);
        }

        public IResponse VisitStartViewChangeXL(StartViewChangeXL startViewChange) {
            if (startViewChange.ViewNumber <= this.replicaState.ViewNumber) {
                return null;
            }
            if (startViewChange.ViewNumber == this.viewNumber &&
                ConfigurationUtils.CompareConfigurations(startViewChange.Configuration, this.configuration)) {
                return new StartViewChangeXLOk(this.replicaState.ServerId, this.viewNumber, this.configuration);
            }
            Log.Debug("Received Start View Change that don't match.");
            return null;
        }

        public IResponse VisitDoViewChangeXL(DoViewChangeXL doViewChange) {
            if (doViewChange.ViewNumber <= this.replicaState.ViewNumber) {
                return null;
            }
            if (this.imTheManager &&
                doViewChange.ViewNumber == this.viewNumber &&
                ConfigurationUtils.CompareConfigurations(doViewChange.Configuration, this.configuration)) {
                Interlocked.Increment(ref this.messagesDoViewChange);

                if (doViewChange.CommitNumber > this.bestDoViewChange.CommitNumber) {
                    this.bestDoViewChange = doViewChange;
                }


                this.CheckNumberAndSetNewConfiguration();
            }

            return null;
        }

        public IResponse VisitStartChangeXL(StartChangeXL startChange) {
            if (startChange.ViewNumber <= this.replicaState.ViewNumber) {
                return null;
            }
            // Set new configuration
            Uri[] replicasUrl = this.configuration.Values
                .Where(url => !url.Equals(this.replicaState.MyUrl))
                .ToArray();
            this.replicaState.SetNewConfiguration(
                startChange.Configuration,
                replicasUrl,
                startChange.ViewNumber,
                startChange.TupleSpace,
                startChange.ClientTable,
                startChange.CommitNumber);
            this.replicaState.ChangeToNormalState();
            return null;
        }

        private void MulticastStartViewChange() {
            IMessage message = new StartViewChangeXL(this.replicaState.ServerId, this.viewNumber, this.configuration);
            Uri[] currentConfiguration = this.replicaState.ReplicasUrl.ToArray();

            IResponses responses = this.messageServiceClient.RequestMulticast(
                message,
                currentConfiguration,
                this.replicaState.Configuration.Count / 2,
                (int)(Timeout.TIMEOUT_VIEW_CHANGE),
                true);

            IResponse[] responsesVector = responses.ToArray();

            // There was no quorum to accept the view change
            if (responsesVector.Length < this.numberToWait) {
                Log.Debug($"There was no quorum for view change. " +
                         $"Just received {responsesVector.Length} from at least {this.numberToWait}");
                this.replicaState.ChangeToNormalState();
                return;
            }

            // In case I'm the manager, wait for f DoViewChange
            if (this.imTheManager) {
                this.CheckNumberAndSetNewConfiguration();
            } else {
                // Else, send DoViewChange to leader
                Uri leader = this.configuration.Values.ToArray()[0];
                IMessage doViewMessage = new DoViewChangeXL(
                    this.replicaState.ServerId,
                    this.viewNumber,
                    this.replicaState.ViewNumber,
                    this.configuration,
                    this.replicaState.TupleSpace,
                    this.replicaState.ClientTable,
                    this.replicaState.CommitNumber);

                this.messageServiceClient.Request(doViewMessage, leader, -1);
            }
        }


        private void CheckNumberAndSetNewConfiguration() {
            if (this.messagesDoViewChange >= this.numberToWait) {
                // start change
                Uri[] replicasUrl = this.configuration.Values
                    .Where(url => !url.Equals(this.replicaState.MyUrl))
                    .ToArray();

                IMessage message = new StartChangeXL(
                    this.replicaState.ServerId,
                    this.viewNumber,
                    this.configuration,
                    this.bestDoViewChange.TupleSpace,
                    this.bestDoViewChange.ClientTable,
                    this.bestDoViewChange.CommitNumber);
                Task.Factory.StartNew(() =>
                    this.messageServiceClient.RequestMulticast(message, replicasUrl, replicasUrl.Length, -1, false));

                // Set new configuration
                this.replicaState.SetNewConfiguration(
                    this.bestDoViewChange.Configuration,
                    replicasUrl,
                    this.bestDoViewChange.ViewNumber,
                    this.bestDoViewChange.TupleSpace,
                    this.bestDoViewChange.ClientTable,
                    this.bestDoViewChange.CommitNumber);

            }
        }

        private void StartTimeout() {
            Thread.Sleep((int)(Timeout.TIMEOUT_VIEW_CHANGE));
            if (this.Equals(this.replicaState.State)) {
                // View Change was not successful, return to normal
                Log.Debug("View Change was not successful.");
                this.replicaState.ChangeToNormalState();
            }
        }

        private IResponse WaitNormalState(IMessage message) {
            while (!(this.replicaState.State is NormalStateMessageProcessor)) {
                this.replicaState.HandlerStateChanged.WaitOne();
            }
            return message.Accept(this.replicaState.State);
        }

        public override string ToString() {
            return "View Change";
        }
    }
}