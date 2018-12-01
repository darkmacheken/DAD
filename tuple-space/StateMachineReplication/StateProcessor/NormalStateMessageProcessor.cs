
using System;
using System.Linq;
using System.Threading;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;

namespace StateMachineReplication.StateProcessor {
    internal enum ProcessRequest { DROP, LAST_EXECUTION}

    public class NormalStateMessageProcessor : IMessageSMRVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(NormalStateMessageProcessor));

        private readonly MessageServiceClient messageServiceClient;
        private readonly ReplicaState replicaState;

        public NormalStateMessageProcessor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.messageServiceClient = messageServiceClient;
            this.replicaState = replicaState;
        }

        public IResponse VisitAddRequest(AddRequest addRequest) {
            if (!this.replicaState.IAmTheLeader()) {
                // I'm not the leader. 
                // TODO: return who's the leader
                return null;
            }

            ProcessRequest runProcessRequestProtocol = this.RunProcessRequestProtocol(addRequest, new AddExecutor(addRequest));
            if (runProcessRequestProtocol == ProcessRequest.DROP) {
                return null;
            }

            if (runProcessRequestProtocol == ProcessRequest.LAST_EXECUTION) {
                return this.replicaState.ClientTable[addRequest.ClientId].Item2;
            }

            return null;
        }

        public IResponse VisitTakeRequest(TakeRequest takeRequest) {
            if (!this.replicaState.IAmTheLeader()) {
                // I'm not the leader. 
                // TODO: return who's the leader
                return null;
            }

            ProcessRequest runProcessRequestProtocol = this.RunProcessRequestProtocol(takeRequest, new TakeExecutor(takeRequest));
            if (runProcessRequestProtocol == ProcessRequest.DROP) {
                return null;
            }

            if (runProcessRequestProtocol == ProcessRequest.LAST_EXECUTION) {
                return this.replicaState.ClientTable[takeRequest.ClientId].Item2;
            }

            return null;
        }

        public IResponse VisitReadRequest(ReadRequest readRequest) {
            if (!this.replicaState.IAmTheLeader()) {
                // I'm not the leader. 
                //TODO: return who's the leader
                return null;
            }

            ProcessRequest runProcessRequestProtocol = this.RunProcessRequestProtocol(readRequest, new ReadExecutor(readRequest));
            if (runProcessRequestProtocol == ProcessRequest.DROP) {
                return null;
            }

            if (runProcessRequestProtocol == ProcessRequest.LAST_EXECUTION) {
                return this.replicaState.ClientTable[readRequest.ClientId].Item2;
            }

            return null;
        }

        public IResponse VisitPrepareMessage(PrepareMessage prepareMessage) {
            AutoResetEvent myHandler = new AutoResetEvent(false);
            this.replicaState.HandlersPrepare.TryAdd(prepareMessage.OpNumber, myHandler);
            // It must wait for previous messages.
            while (this.replicaState.OpNumber != (prepareMessage.OpNumber - 1)) {
                myHandler.WaitOne();
            }
            this.replicaState.HandlersPrepare.TryRemove(prepareMessage.OpNumber, out myHandler);

            int opNumber;
            int replicaView;
            lock (this.replicaState) {
                replicaView = this.replicaState.ViewNumber;
                opNumber = this.replicaState.IncrementOpNumberNumber();
                this.replicaState.Logger.Add(prepareMessage.ClientRequest);
            }

            // Notify all threads that are waiting for new prepare messages
            foreach (AutoResetEvent eventHandler in this.replicaState.HandlersCommits.Values) {
                eventHandler.Set();
            }
            foreach (AutoResetEvent eventHandler in this.replicaState.HandlersPrepare.Values) {
                eventHandler.Set();
            }

            return new PrepareOk(this.replicaState.ServerId, replicaView, opNumber);
        }

        public IResponse VisitCommitMessage(CommitMessage commitMessage) {
            AutoResetEvent myHandler = new AutoResetEvent(false);
            this.replicaState.HandlersCommits.TryAdd(commitMessage.CommitNumber, myHandler);
            // It must confirm that it received the prepare message.
            while (commitMessage.CommitNumber > this.replicaState.OpNumber) {
                myHandler.WaitOne();
            }

            this.replicaState.HandlersCommits.TryRemove(commitMessage.CommitNumber, out myHandler);

            ClientRequest request = this.replicaState.Logger[commitMessage.CommitNumber];
            Log.Debug($"Requesting {request} to Tuple Space.");

            Executor requestExecutor = null;
            if (request is AddRequest) {
                requestExecutor = new AddExecutor(request);
            } else if (request is TakeRequest) {
                requestExecutor = new TakeExecutor(request);
            } else if (request is ReadRequest) {
                requestExecutor = new ReadExecutor(request);
            }

            // Update Client Table
            this.replicaState.ClientTable[request.ClientId] =
                new Tuple<int, ClientResponse>(request.RequestNumber, requestExecutor);

            // Add Request to Queue to be executed
            this.replicaState.ExecutionQueue.Add(requestExecutor);
            return null;
        }

        public IResponse VisitHandShakeRequest(HandShakeRequest handShakeRequest) {
            Uri[] viewConfiguration = this.replicaState.Configuration.Values.ToArray();
            return new HandShakeResponse(Protocol.StateMachineReplication, this.replicaState.ViewNumber, viewConfiguration);
        }

        private ProcessRequest RunProcessRequestProtocol(ClientRequest clientRequest, Executor clientExecutor) {
            if (this.replicaState.ClientTable.TryGetValue(clientRequest.ClientId, out Tuple<int, ClientResponse> clientResponse)) {
                // Key is in the dictionary
                if (clientResponse == null || clientResponse.Item1 < 0 ||
                    clientRequest.RequestNumber < clientResponse.Item1) {
                    // Duplicate Request: Long forgotten => drop
                    return ProcessRequest.DROP;
                }

                if (clientRequest.RequestNumber == clientResponse.Item1) {
                    // Duplicate Request
                    // If it is in execution.. wait.
                    if (clientResponse.Item2.GetType() == typeof(Executor)) {
                        Executor executor = (Executor)clientResponse.Item2;
                        executor.Executed.WaitOne();
                    }
                    return ProcessRequest.LAST_EXECUTION;
                }
            } else {
                // Not in dictionary... Add with value as null
                this.replicaState.ClientTable.Add(clientRequest.ClientId, new Tuple<int, ClientResponse>(-1, null));
            }

            // Send Prepare Message and waits for f replies. opNumber is the order we agreed upon.
            int opNumber = this.SendPrepareMessage(clientRequest);
            clientExecutor.OpNumber = opNumber;

            // Update Client Table With status execution
            this.replicaState.ClientTable[clientRequest.ClientId] =
                new Tuple<int, ClientResponse>(clientRequest.RequestNumber, clientExecutor);

            // Add to execution queue
            this.replicaState.ExecutionQueue.Add(clientExecutor);

            // wait execution
            clientExecutor.Executed.WaitOne();

            return ProcessRequest.LAST_EXECUTION;
        }

        private int SendPrepareMessage(ClientRequest clientRequest) {
            int viewNumber;
            int opNumber;
            int commitNumber;
            Uri[] replicasUrls;

            lock (this.replicaState) {
                viewNumber = this.replicaState.ViewNumber;
                commitNumber = this.replicaState.CommitNumber;
                opNumber = this.replicaState.IncrementOpNumberNumber();
                this.replicaState.Logger.Add(clientRequest);
                replicasUrls = this.replicaState.ReplicasUrl.ToArray();
            }

            PrepareMessage prepareMessage = new PrepareMessage(
                this.replicaState.ServerId,
                viewNumber,
                clientRequest,
                opNumber,
                commitNumber);

            // Wait for (Number backup replicas + the leader) / 2
            int f = (replicasUrls.Length + 1) / 2;
            this.messageServiceClient.RequestMulticast(prepareMessage, replicasUrls, f, -1);

            return opNumber;
        }

    }
}