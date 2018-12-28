using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;
using Timeout = MessageService.Timeout;

namespace XuLiskovAdvanced.StateProcessor {
    internal enum ProcessRequest { DROP, LAST_EXECUTION }

    public class NormalStateMessageProcessor : IMessageXLVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(NormalStateMessageProcessor));

        private readonly ReplicaState replicaState;
        private readonly MessageServiceClient messageServiceClient;

        public NormalStateMessageProcessor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.replicaState = replicaState;
            this.messageServiceClient = messageServiceClient;

            Log.Info("Changed to Normal State.");
        }

        public IResponse VisitAddRequest(AddRequest addRequest) {
            return ExecuteRequest(addRequest, new AddExecutor(addRequest));
        }

        public IResponse VisitTakeRequest(TakeRequest takeRequest) {
            return ExecuteRequest(takeRequest, new TakeExecutor(takeRequest));
        }

        public IResponse VisitReadRequest(ReadRequest readRequest) {
            return ExecuteRequest(readRequest, new ReadExecutor(readRequest));
        }

        public IResponse VisitGetAndLock(GetAndLockRequest getAndLockRequest) {
            return ExecuteRequest(getAndLockRequest, new GetAndLockExecutor(getAndLockRequest));
        }

        public IResponse VisitUnlockRequest(UnlockRequest unlockRequest) {
            return ExecuteRequest(unlockRequest, new UnlockExecutor(unlockRequest));
        }

        public IResponse VisitStartViewChangeXL(StartViewChangeXL startViewChange) {
            if (this.replicaState.HandlerStateChanged.WaitOne(Timeout.TIMEOUT_VIEW_CHANGE) &&
                this.replicaState.State is ViewChangeMessageProcessor) {
                return startViewChange.Accept(this.replicaState.State);
            }
            return null;
        }

        public IResponse VisitDoViewChangeXL(DoViewChangeXL doViewChange) {
            if (doViewChange.ViewNumber <= this.replicaState.ViewNumber) {
                return null;
            }
            lock (this.replicaState.State) {
                if (!(this.replicaState.State is ViewChangeMessageProcessor)) {
                    this.replicaState.ChangeToViewChange(doViewChange.ViewNumber, doViewChange.Configuration);
                }
            }
            return doViewChange.Accept(this.replicaState.State);
        }

        public IResponse VisitStartChangeXL(StartChangeXL startChange) {
            if (startChange.ViewNumber <= this.replicaState.ViewNumber) {
                return null;
            }
            lock (this.replicaState.State) {
                if (!(this.replicaState.State is ViewChangeMessageProcessor)) {
                    this.replicaState.ChangeToViewChange(startChange);
                }
            }
            return startChange.Accept(this.replicaState.State);
        }

        public IResponse VisitClientHandShakeRequest(ClientHandShakeRequest clientHandShakeRequest) {
            Uri[] viewConfiguration = this.replicaState.Configuration.Values.ToArray();
            return new ClientHandShakeResponse(Protocol.XuLiskov, this.replicaState.ViewNumber, viewConfiguration);
        }

        public IResponse VisitServerHandShakeRequest(ServerHandShakeRequest serverHandShakeRequest) {
            return new ServerHandShakeResponse(this.replicaState.Configuration.Values.ToArray());
        }

        public IResponse VisitJoinView(JoinView joinView) {
            Log.Info($"JoinView issued for server {joinView.ServerId}");
            int newViewNumber = this.replicaState.ViewNumber + 1;
            if (this.replicaState.Configuration.ContainsKey(joinView.ServerId)) {
                return null;
            }
            SortedDictionary<string, Uri> newConfiguration = new SortedDictionary<string, Uri>(this.replicaState.Configuration);
            newConfiguration.Add(joinView.ServerId, joinView.Url);

            this.replicaState.ChangeToViewChange(newViewNumber, newConfiguration);
            return null;
        }

        public IResponse VisitHeartBeat(HeartBeat heartBeat) {
            return this.replicaState.UpdateHeartBeat(heartBeat.ServerId);
        }

        private IResponse ExecuteRequest(ClientRequest clientRequest, Executor clientExecutor) {
            if (clientRequest.ViewNumber < this.replicaState.ViewNumber) {
                // Old view
                Log.Debug("ExecuteRequest: DROP - old view");
                return null;
            }

            if (clientRequest.ViewNumber > this.replicaState.ViewNumber) {
                // There's a higher view that I'm not in
                this.replicaState.ChangeToInitializationState();
                return null;
            }
            ProcessRequest runProcessRequestProtocol = this.RunProcessRequestProtocol(clientRequest, clientExecutor);
            if (runProcessRequestProtocol == ProcessRequest.DROP) {
                return null;
            }

            if (runProcessRequestProtocol == ProcessRequest.LAST_EXECUTION) {
                return this.replicaState.ClientTable[clientRequest.ClientId].Item2;
            }

            return null;
        }

        private ProcessRequest RunProcessRequestProtocol(ClientRequest clientRequest, Executor clientExecutor) {
            if (this.replicaState.ClientTable.TryGetValue(clientRequest.ClientId, out Tuple<int, ClientResponse> clientResponse)) {
                // Key is in the dictionary
                if (clientResponse == null || clientResponse.Item1 < 0 ||
                    clientRequest.RequestNumber < clientResponse.Item1) {
                    // Duplicate Request: Long forgotten => drop
                    Log.Debug($"Duplicate Request {clientRequest}: DROP");
                    return ProcessRequest.DROP;
                }

                if (clientRequest.RequestNumber == clientResponse.Item1) {
                    // Duplicate Request
                    Log.Debug($"Duplicate Request {clientRequest}: LAST_EXECUTION");
                    return ProcessRequest.LAST_EXECUTION;
                }

            } else {
                // Not in dictionary... Add with value as null
                this.replicaState.ClientTable.Add(clientRequest.ClientId, new Tuple<int, ClientResponse>(-1, null));
                this.replicaState.HandlersClient.TryAdd(clientRequest.ClientId, new EventWaitHandle(false, EventResetMode.ManualReset));
            }

            // Execute request
            clientExecutor.Execute(this.replicaState.RequestsExecutor);

            // Notify threads waiting
            this.replicaState.HandlersClient[clientRequest.ClientId].Set();
            this.replicaState.HandlersClient[clientRequest.ClientId].Reset();

            if (this.replicaState.ClientTable[clientRequest.ClientId].Item1 != clientRequest.RequestNumber) {
                Log.Debug($"Drop: client request is different. {this.replicaState.ClientTable[clientRequest.ClientId].Item1}" +
                          $" != {clientRequest.RequestNumber}");
                return ProcessRequest.DROP;
            }

            return ProcessRequest.LAST_EXECUTION;
        }

        public override string ToString() {
            return "Normal";
        }
    }
}