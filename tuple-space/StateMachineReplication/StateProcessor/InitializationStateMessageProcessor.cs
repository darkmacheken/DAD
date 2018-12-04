using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;

namespace StateMachineReplication.StateProcessor {
    public class InitializationStateMessageProcessor : IMessageSMRVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(InitializationStateMessageProcessor));

        private readonly MessageServiceClient messageServiceClient;
        private readonly ReplicaState replicaState;

        private const string SERVERS_LIST = "..\\..\\..\\servers.txt";

        public InitializationStateMessageProcessor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.messageServiceClient = messageServiceClient;
            this.replicaState = replicaState;

            Log.Info("Changed to Initialization State.");

            Task.Factory.StartNew(this.InitProtocol);
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
            lock (this) {
                if (!(this.replicaState.State is ViewChangeMessageProcessor)) {
                    this.replicaState.ChangeToViewChange(doViewChange);
                }
            }
            return doViewChange.Accept(this.replicaState.State);
        }

        public IResponse VisitStartChange(StartChange startChange) {
            Log.Info($"Start Change issued from server {startChange.ServerId}");
            lock (this) {
                if (!(this.replicaState.State is ViewChangeMessageProcessor)) {
                    this.replicaState.ChangeToViewChange(startChange);
                }
            }
            return startChange.Accept(this.replicaState.State);
        }

        public IResponse VisitRecovery(Recovery recovery) {
            return this.WaitNormalState(recovery);
        }
        
        private IResponse WaitNormalState(IMessage message) {
            while (!(this.replicaState.State is NormalStateMessageProcessor)) {
                this.replicaState.HandlerStateChanged.WaitOne();
            }
            return message.Accept(this.replicaState.State);
        }

        private void InitProtocol() {
            Uri[] servers = System.IO.File.ReadAllLines(SERVERS_LIST).ToList()
                .ConvertAll<Uri>(server => new Uri(server))
                .Where(server => !server.Equals(this.replicaState.myUrl))
                .ToArray();

            IMessage message = new ServerHandShakeRequest(this.replicaState.ServerId, Protocol.StateMachineReplication);
            IResponses responses =
                this.messageServiceClient.RequestMulticast(message, servers, servers.Length, Timeout.TIMEOUT_SERVER_HANDSHAKE);

            IResponse[] filteredResponses = responses.ToArray().AsEnumerable()
                .Where(response => response != null)
                .ToArray();

            // I'm the first server
            if (filteredResponses.Length == 0) {
                Log.Info("No servers found for handshake.");
                this.replicaState.SetNewConfiguration(
                    new SortedDictionary<string, Uri> { { this.replicaState.ServerId, this.replicaState.myUrl } },
                    new Uri[] { },
                    0);
                this.replicaState.ChangeToNormalState();
                return;
            }

            // Else multicast joinView to everyone
            Uri[] configuration = ((ServerHandShakeResponse)filteredResponses[0]).ViewConfiguration;
            IMessage joinViewMessage = new JoinView(this.replicaState.ServerId, this.replicaState.myUrl);
            this.messageServiceClient.RequestMulticast(
                joinViewMessage,
                configuration,
                configuration.Length,
                -1);
        }

        public override string ToString() {
            return "Initialization";
        }
    }
}