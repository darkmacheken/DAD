using System;
using System.Collections.Generic;
using System.Linq;

using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;
using StateMachineReplication;

namespace XuLiskov.StateProcessor {
    public class NormalStateMessageProcessor : IMessageVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(NormalStateMessageProcessor));

        private ReplicaState replicaState;
        private MessageServiceClient messageServiceClient;

        public NormalStateMessageProcessor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.replicaState = replicaState;
            this.messageServiceClient = messageServiceClient;
        }

        public IResponse VisitAddRequest(AddRequest addRequest) {
            int runProcessRequestProtocol = this.RunProcessRequestProtocol(addRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[addRequest.ClientId].Item2;
            } else {
                Log.Debug($"Requesting Add({addRequest.Tuple}) to Tuple Space.");
                this.replicaState.TupleSpace.Add(addRequest.Tuple);

                int viewNumber = this.replicaState.ViewNumber;

                ClientResponse clientResponse = new ClientResponse(addRequest.RequestNumber, viewNumber, string.Empty);
                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[addRequest.ClientId] =
                        new Tuple<int, ClientResponse>(addRequest.RequestNumber, clientResponse);
                }

                return clientResponse;
            }
        }

        public IResponse VisitTakeRequest(TakeRequest takeRequest) {
            int runProcessRequestProtocol = this.RunProcessRequestProtocol(takeRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[takeRequest.ClientId].Item2;
            } else {
                Log.Debug($"Requesting Take({takeRequest.Tuple}) to Tuple Space.");
                TupleSpace.Tuple takeTuple = this.replicaState.TupleSpace.UnlockAndTake(
                    takeRequest.ClientId, 
                    takeRequest.RequestNumber, 
                    takeRequest.Tuple);

                int viewNumber = this.replicaState.ViewNumber;

                ClientResponse clientResponse = new ClientResponse(
                    takeRequest.RequestNumber, 
                    viewNumber, 
                    takeRequest.ToString());
                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[takeRequest.ClientId] =
                        new Tuple<int, ClientResponse>(takeRequest.RequestNumber, clientResponse);
                }
                return clientResponse;
            }
        }

        public IResponse VisitReadRequest(ReadRequest readRequest) {
            int runProcessRequestProtocol = this.RunProcessRequestProtocol(readRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[readRequest.ClientId].Item2;
            } else {
                Log.Debug($"Requesting Read({readRequest.Tuple}) to Tuple Space.");
                TupleSpace.Tuple readTuple = this.replicaState.TupleSpace.Read(readRequest.Tuple);

                int viewNumber = this.replicaState.ViewNumber;

                ClientResponse clientResponse = new ClientResponse(readRequest.RequestNumber, viewNumber, readTuple.ToString());
                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[readRequest.ClientId] =
                        new Tuple<int, ClientResponse>(readRequest.RequestNumber, clientResponse);
                }
                return clientResponse;
            }
        }

        public IResponse VisitGetAndLock(GetAndLockRequest getAndLockRequest) {
            int runProcessRequestProtocol = this.RunProcessRequestProtocol(getAndLockRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[getAndLockRequest.ClientId].Item2;
            } else {
                Log.Debug($"Requesting GetAndLock({getAndLockRequest.Tuple}) to Tuple Space.");
                List<string> tuples = this.replicaState.TupleSpace.GetAndLock(
                    getAndLockRequest.ClientId,
                    getAndLockRequest.RequestNumber,
                    getAndLockRequest.Tuple);

                int viewNumber = this.replicaState.ViewNumber;

                GetAndLockResponse clientResponse = new 
                    GetAndLockResponse(getAndLockRequest.RequestNumber, viewNumber, tuples);
                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[getAndLockRequest.ClientId] =
                        new Tuple<int, ClientResponse>(getAndLockRequest.RequestNumber, clientResponse);
                }
                return clientResponse;
            }
        }

        public IResponse VisitUnlockRequest(UnlockRequest unlockRequest) {
            int runProcessRequestProtocol = this.RunProcessRequestProtocol(unlockRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[unlockRequest.ClientId].Item2;
            } else {
                Log.Debug($"Requesting Unlock({unlockRequest.Tuple}) to Tuple Space.");
                this.replicaState.TupleSpace.Unlock(unlockRequest.ClientId, unlockRequest.RequestNumber);

                int viewNumber = this.replicaState.ViewNumber;

                ClientResponse clientResponse = new 
                    ClientResponse(unlockRequest.RequestNumber, viewNumber, string.Empty);
                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[unlockRequest.ClientId] =
                        new Tuple<int, ClientResponse>(unlockRequest.RequestNumber, clientResponse);
                }
                return clientResponse;
            }
        }

        public IResponse VisitHandShakeRequest(HandShakeRequest handShakeRequest) {
            Uri[] viewConfiguration = this.replicaState.Configuration.Values.ToArray();
            return new HandShakeResponse(Protocol.XuLiskov, this.replicaState.ViewNumber, viewConfiguration);
        }

        private int RunProcessRequestProtocol(ClientRequest clientRequest) {
            if (this.replicaState.ClientTable.TryGetValue(clientRequest.ClientId, out Tuple<int, ClientResponse> clientResponse)) {
                // Key is in the dictionary
                if (clientResponse.Item1 < 0 ||
                    clientRequest.RequestNumber < clientResponse.Item1) {
                    // Duplicate Request: Long forgotten => drop
                    return -1;
                } else if (clientRequest.RequestNumber == clientResponse.Item1) {
                    // Duplicate Request: Last Execution => return stored value in the client table
                    return 0;
                }
            } else {
                // Not in dictionary... Add with value as null
                this.replicaState.ClientTable.Add(clientRequest.ClientId, new Tuple<int, ClientResponse>(-1, null));
            }

            // We can return now, it's our turn to execute.
            return 1;
        }


        // TODO: make a better design to not implement this. This belongs to SMR. It will be
        // TODO: in the end of the file to not make confusion.
        public IResponse VisitPrepareMessage(PrepareMessage prepareMessage) {
            throw new System.NotImplementedException();
        }

        public IResponse VisitCommitMessage(CommitMessage commitMessage) {
            throw new System.NotImplementedException();
        }
    }
}