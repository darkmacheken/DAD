using System;
using System.Threading.Tasks;
using MessageService;
using MessageService.Serializable;

namespace StateMachineReplication {
    public class RequestsExecutor : IExecutorVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RequestsExecutor));

        private readonly ReplicaState replicaState;
        private readonly MessageServiceClient messageServiceClient;

        public RequestsExecutor(ReplicaState replicaState, MessageServiceClient messageServiceClient) {
            this.replicaState = replicaState;
            this.messageServiceClient = messageServiceClient;
        }

        public void ExecuteAdd(AddExecutor addExecutor) {
            Log.Debug($"Requesting Add({addExecutor.Tuple}) to Tuple Space.");
            this.replicaState.TupleSpace.Add(addExecutor.Tuple);

            // increment commit number
            int commitNumber = this.replicaState.IncrementCommitNumber();
            int viewNumber = this.replicaState.ViewNumber;

            ClientResponse clientResponse = new ClientResponse(commitNumber, viewNumber, string.Empty);

            // update client table
            lock (this.replicaState) {
                this.replicaState.ClientTable[addExecutor.ClientId] =
                    new Tuple<int, ClientResponse>(addExecutor.RequestNumber, clientResponse);
            }

            // Signal waiting thread that the execution is done
            addExecutor.Executed.Set();
            this.replicaState.HandlersCommits.Set();
            this.replicaState.HandlersCommits.Reset();

            // commit execution
            this.SendCommit(viewNumber, commitNumber);
        }

        public void ExecuteTake(TakeExecutor takeExecutor) {
            Log.Debug($"Requesting Take({takeExecutor.Tuple}) to Tuple Space.");
            TupleSpace.Tuple takeTuple = this.replicaState.TupleSpace.Take(takeExecutor.Tuple);

            // increment commit number
            int commitNumber = this.replicaState.IncrementCommitNumber();
            int viewNumber = this.replicaState.ViewNumber;

            ClientResponse clientResponse;
            if (takeTuple == null) {
                clientResponse = new ClientResponse(commitNumber, viewNumber, null);
            } else {
                clientResponse = new ClientResponse(commitNumber, viewNumber, takeTuple.ToString());
            }
             
            // update client table
            lock (this.replicaState) {
                this.replicaState.ClientTable[takeExecutor.ClientId] =
                    new Tuple<int, ClientResponse>(takeExecutor.RequestNumber, clientResponse);
            }

            // Signal waiting thread that the execution is done
            takeExecutor.Executed.Set();
            this.replicaState.HandlersCommits.Set();
            this.replicaState.HandlersCommits.Reset();

            // commit execution
            this.SendCommit(viewNumber, commitNumber);
        }

        public void ExecuteRead(ReadExecutor readExecutor) {
            Log.Debug($"Requesting Read({readExecutor.Tuple}) to Tuple Space.");
            TupleSpace.Tuple readTuple = this.replicaState.TupleSpace.Read(readExecutor.Tuple);

            // increment commit number
            int commitNumber = this.replicaState.IncrementCommitNumber();
            int viewNumber = this.replicaState.ViewNumber;

            ClientResponse clientResponse;
            if (readTuple == null) {
                clientResponse = new ClientResponse(commitNumber, viewNumber, null);
            } else {
                clientResponse = new ClientResponse(commitNumber, viewNumber, readTuple.ToString());
            }
            // update client table
            lock (this.replicaState) {
                this.replicaState.ClientTable[readExecutor.ClientId] =
                    new Tuple<int, ClientResponse>(readExecutor.RequestNumber, clientResponse);
            }

            // Signal waiting thread that the execution is done
            readExecutor.Executed.Set();
            this.replicaState.HandlersCommits.Set();
            this.replicaState.HandlersCommits.Reset();

            // commit execution
            this.SendCommit(viewNumber, commitNumber);
        }

        private void SendCommit(int viewNumber, int commitNumber) {
            if (!this.replicaState.IAmTheLeader()) {
                return;
            }
            Uri[] replicasUrls;

            lock (this.replicaState) {
                replicasUrls = this.replicaState.ReplicasUrl.ToArray();
            }

            CommitMessage commitMessage = new CommitMessage(this.replicaState.ServerId, viewNumber, commitNumber);

            // Don't wait for response
            Task.Factory.StartNew(() => 
                this.messageServiceClient.RequestMulticast(commitMessage, replicasUrls, replicasUrls.Length, -1, false));
        }
    }
}