using System;
using System.Linq;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;
using XuLiskov;

namespace XuLiskov.StateProcessor {
    internal enum ProcessRequest { DROP, LAST_EXECUTION }

    public class NormalStateMessageProcessor : IMessageXLVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(NormalStateMessageProcessor));

        private readonly ReplicaState replicaState;
        private readonly MessageServiceClient messageServiceClient;

        public NormalStateMessageProcessor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.replicaState = replicaState;
            this.messageServiceClient = messageServiceClient;
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

        public IResponse VisitClientHandShakeRequest(ClientHandShakeRequest clientHandShakeRequest) {
            Uri[] viewConfiguration = this.replicaState.Configuration.Values.ToArray();
            return new ClientHandShakeResponse(Protocol.XuLiskov, this.replicaState.ViewNumber, viewConfiguration);
        }

        public IResponse VisitServerHandShakeRequest(ServerHandShakeRequest serverHandShakeRequest) {
            throw new NotImplementedException();
        }

        public IResponse VisitJoinView(JoinView joinView) {
            throw new NotImplementedException();
        }

        private IResponse ExecuteRequest(ClientRequest clientRequest, Executor clientExecutor) {
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

            // Update Client Table With status execution
            this.replicaState.ClientTable[clientRequest.ClientId] =
                new Tuple<int, ClientResponse>(clientRequest.RequestNumber, clientExecutor);

            // Execute in a new thread
            Task.Factory.StartNew(() => clientExecutor.Execute(this.replicaState.RequestsExecutor));

            // wait execution
            clientExecutor.Executed.WaitOne();

            return ProcessRequest.LAST_EXECUTION;
        }
    }
}