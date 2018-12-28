using System;
using System.Collections.Generic;
using MessageService;
using MessageService.Serializable;

namespace XuLiskovAdvanced {
    public class RequestsExecutor : IExecutorXLVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RequestsExecutor));

        private readonly ReplicaState replicaState;

        public RequestsExecutor(ReplicaState replicaState) {
            this.replicaState = replicaState;
        }

        public void ExecuteAdd(AddExecutor addExecutor) {
            Log.Debug($"Requesting Add({addExecutor.Tuple}) to Tuple Space.");
            this.replicaState.TupleSpace.Add(addExecutor.Tuple);

            int viewNumber = this.replicaState.ViewNumber;

            ClientResponse clientResponse = new ClientResponse(addExecutor.RequestNumber, viewNumber, string.Empty);

            // update client table
            this.UpdateClientTable(addExecutor, clientResponse);
            this.replicaState.IncrementCommitNumber();

        }
        
        public void ExecuteTake(TakeExecutor takeExecutor) {
            Log.Debug($"Requesting Take({takeExecutor.Tuple}) to Tuple Space.");
            this.replicaState.TupleSpace.UnlockAndTake(takeExecutor.ClientId, takeExecutor.Tuple);

            int viewNumber = this.replicaState.ViewNumber;

            ClientResponse clientResponse = new ClientResponse(
                takeExecutor.RequestNumber,
                viewNumber,
                takeExecutor.Tuple);
            
            // update client table
            this.UpdateClientTable(takeExecutor, clientResponse);
            this.replicaState.IncrementCommitNumber();

        }

        public void ExecuteRead(ReadExecutor readExecutor) {
            Log.Debug($"Requesting Read({readExecutor.Tuple}) to Tuple Space.");
            TupleSpace.Tuple readTuple = this.replicaState.TupleSpace.Read(readExecutor.Tuple);

            int viewNumber = this.replicaState.ViewNumber;

            string tuple = null;
            if (readTuple != null) {
                tuple = readTuple.ToString();
            }
            ClientResponse clientResponse = new ClientResponse(readExecutor.RequestNumber, viewNumber, tuple);

            // update client table
            this.UpdateClientTable(readExecutor, clientResponse);
            this.replicaState.IncrementCommitNumber();

        }

        public void ExecuteGetAndLock(GetAndLockExecutor getAndLockExecutor) {
            Log.Debug($"Requesting GetAndLock({getAndLockExecutor.Tuple}) to Tuple Space.");
            List<string> tuples = this.replicaState.TupleSpace.GetAndLock(getAndLockExecutor.ClientId, getAndLockExecutor.Tuple);

            if (tuples == null) {
                // Got refused
                return;
            }

            int viewNumber = this.replicaState.ViewNumber;

            GetAndLockResponse clientResponse = new GetAndLockResponse(
                this.replicaState.ServerId,
                getAndLockExecutor.RequestNumber, 
                viewNumber, 
                tuples);

            // update client table
            this.UpdateClientTable(getAndLockExecutor, clientResponse);
            this.replicaState.IncrementCommitNumber();

        }

        public void ExecuteUnlock(UnlockExecutor unlockExecutor) {
            Log.Debug($"Requesting Unlock({unlockExecutor.Tuple}) to Tuple Space.");
            this.replicaState.TupleSpace.Unlock(unlockExecutor.ClientId);

            int viewNumber = this.replicaState.ViewNumber;

            ClientResponse clientResponse = new
                ClientResponse(unlockExecutor.RequestNumber, viewNumber, string.Empty);

            // update client table
            UpdateClientTable(unlockExecutor, clientResponse);
            this.replicaState.IncrementCommitNumber();

        }

        private void UpdateClientTable(Executor executor, ClientResponse clientResponse) {
            lock (this.replicaState) {
                this.replicaState.ClientTable[executor.ClientId] =
                    new Tuple<int, ClientResponse>(executor.RequestNumber, clientResponse);
            }
        }
    }
}