using System;
using System.Linq;
using System.Threading;

using MessageService;
using MessageService.Serializable;
using MessageService.Visitor;

namespace StateMachineReplication.StateProcessor {
    public class NormalStateMessageProcessor : IMessageVisitor {
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

            int runProcessRequestProtocol = this.RunProcessRequestProtocol(addRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[addRequest.ClientId].Item2;
            } else {
                // TODO: Call upper Layer
                Log.Debug($"Requesting Add({addRequest.Tuple}) to Tuple Space.");

                // increment commit number
                int commitNumber = this.replicaState.IncrementCommitNumber();
                int viewNumber = this.replicaState.ViewNumber;

                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[addRequest.ClientId] = new Tuple<int, ClientResponse>(addRequest.RequestNumber, null);
                }
                // commit execution
                this.SendCommit(viewNumber, commitNumber);
                return null;
            }
        }

        public IResponse VisitTakeRequest(TakeRequest takeRequest) {
            if (!this.replicaState.IAmTheLeader()) {
                // I'm not the leader. 
                // TODO: return who's the leader
                return null;
            }

            int runProcessRequestProtocol = this.RunProcessRequestProtocol(takeRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[takeRequest.ClientId].Item2;
            } else {
                // TODO: Call upper Layer
                Log.Debug($"Requesting Take({takeRequest.Tuple}) to Tuple Space.");

                // increment commit number
                int commitNumber = this.replicaState.IncrementCommitNumber();
                int viewNumber = this.replicaState.ViewNumber;

                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[takeRequest.ClientId] = new Tuple<int, ClientResponse>(takeRequest.RequestNumber, null);
                }
                // commit execution
                this.SendCommit(viewNumber, commitNumber);
                return null;
            }
        }

        public IResponse VisitReadRequest(ReadRequest readRequest) {
            if (!this.replicaState.IAmTheLeader()) {
                // I'm not the leader. 
                //TODO: return who's the leader
                return null;
            }

            int runProcessRequestProtocol = this.RunProcessRequestProtocol(readRequest);
            if (runProcessRequestProtocol < 0) { // Drop
                return null;
            } else if (runProcessRequestProtocol == 0) { // Return last execution's result
                return this.replicaState.ClientTable[readRequest.ClientId].Item2;
            } else {
                // TODO: Call upper Layer
                Log.Debug($"Requesting Read({readRequest.Tuple}) to Tuple Space.");

                // increment commit number
                int commitNumber = this.replicaState.IncrementCommitNumber();
                int viewNumber = this.replicaState.ViewNumber;

                // update client table
                lock (this.replicaState) {
                    this.replicaState.ClientTable[readRequest.ClientId] = new Tuple<int, ClientResponse>(readRequest.RequestNumber, null);
                }
                // commit execution
                this.SendCommit(viewNumber, commitNumber);
                return null;
            }
        }

        public IResponse VisitPrepareMessage(PrepareMessage prepareMessage) {
            if (this.replicaState.OpNumber != (prepareMessage.OpNumber - 1)) {
                // The replica isn't in sync. Some information was lost
                // TODO: Update state. Must block until condition is met.
                throw new NotImplementedException();
            }

            int opNumber;
            int replicaView;
            lock (this.replicaState) {
                replicaView = this.replicaState.ViewNumber;
                opNumber = this.replicaState.IncrementOpNumberNumber();
                this.replicaState.Logger.Add(prepareMessage.ClientRequest);
            }

            return new PrepareOk(this.replicaState.ServerId, replicaView, opNumber);
        }

        public IResponse VisitCommitMessage(CommitMessage commitMessage) {
            // Needs to wait for previous executions or request must arrive.
            // Polling in 25ms in 25ms
            while (this.replicaState.CommitNumber != (commitMessage.CommitNumber - 1) &&
                   this.replicaState.OpNumber >= commitMessage.CommitNumber) {
                Thread.Sleep(25);
            }

            // TODO: Call upper Layer
            Log.Debug($"Requesting {this.replicaState.Logger[commitMessage.CommitNumber]} to Tuple Space.");

            // increment commit number
            this.replicaState.IncrementCommitNumber();
            // update client table
            ClientRequest clientRequest = this.replicaState.Logger[commitMessage.CommitNumber];
            lock (this.replicaState) {
                this.replicaState.ClientTable[clientRequest.ClientId] = new Tuple<int, ClientResponse>(clientRequest.RequestNumber, null);
            }
            return null;
        }

        public IResponse VisitHandShakeRequest(HandShakeRequest handShakeRequest) {
            Uri[] viewConfiguration = this.replicaState.Configuration.Values.ToArray();
            return new HandShakeResponse(Protocol.StateMachineReplication, this.replicaState.ViewNumber, viewConfiguration);
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

            // Send Prepare Message and waits for f replies. opNumber is the order we agreed upon.
            int opNumber = this.SendPrepareMessage(clientRequest);

            // Now we need to execute the request in our turn.
            // Polling in 25ms in 25ms to check if last operation has been committed
            while (this.replicaState.CommitNumber != (opNumber - 1)) {
                Thread.Sleep(25);
            }

            // We can return now, it's our turn to execute.
            return 1;
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
                replicasUrls = this.replicaState.ReplicasUrls.ToArray();
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

        private void SendCommit(int viewNumber, int commitNumber) {
            Uri[] replicasUrls;

            lock (this.replicaState) {
                replicasUrls = this.replicaState.ReplicasUrls.ToArray();
            }
            CommitMessage commitMessage = new CommitMessage(this.replicaState.ServerId, viewNumber, commitNumber);

            // Don't wait for response
            this.messageServiceClient.RequestMulticast(commitMessage, replicasUrls, 0, -1);
        }
    }
}