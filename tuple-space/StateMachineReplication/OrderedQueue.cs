using System;
using System.Collections.Concurrent;
using System.Threading;
using MessageService;
using MessageService.Serializable;

namespace StateMachineReplication {
    public class OrderedQueue {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OrderedQueue));

        private readonly BlockingCollection<Executor> requestsToExecute;
        private readonly EventWaitHandle handler;
        private int lastOpNumber;

        private readonly ReplicaState replicaState;

        public OrderedQueue(ReplicaState replicaState) {
            this.requestsToExecute = new BlockingCollection<Executor>();
            this.handler = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.lastOpNumber = 0;
            this.replicaState = replicaState;
        }
        
        /// <summary>
        /// Adds an element in the queue. It BLOCKS <see langword="while"/> the op number of the element isn't the
        /// next op number of the last added element. If less it returns.
        /// </summary>
        /// <param name="requestToExecute">The element to add.</param>
        public void Add(ClientRequest clientRequest, Executor requestToExecute) {
            if (requestToExecute.OpNumber <= this.lastOpNumber) {
                return;
            }

            Log.Debug($"Execution Queue: Request OpNumber #{requestToExecute.OpNumber}, LastOpNumber #{lastOpNumber}.");
            while (this.lastOpNumber != requestToExecute.OpNumber - 1) {
                this.handler.WaitOne();
            }

            this.requestsToExecute.Add(requestToExecute);
            this.lastOpNumber = requestToExecute.OpNumber;

            // Update Client Table With status execution
            replicaState.ClientTable[clientRequest.ClientId] =
                new Tuple<int, ClientResponse>(clientRequest.RequestNumber, requestToExecute);

            
            Log.Debug($"Added request #{requestToExecute.OpNumber} to Execution Queue.");

            // notify all waiting threads
            this.handler.Set();
            this.handler.Reset();
            replicaState.HandlersClient.TryGetValue(clientRequest.ClientId, out EventWaitHandle handler);
            if (handler != null) {
                handler.Set();
                handler.Reset();
            }
        }

        /// <summary>
        /// Takes the element in the head of the queue. It BLOCKS until there is an element
        /// in the queue.
        /// </summary>
        /// <returns>The element in the top of the queue</returns>
        public Executor Take() {
            return this.requestsToExecute.Take();
        }

        public static void AddRequestToQueue(ReplicaState replicaState, ClientRequest clientRequest, Executor clientExecutor) {
            if (!replicaState.ClientTable.TryGetValue(clientRequest.ClientId, out Tuple<int, ClientResponse> clientTableCell) || 
                clientTableCell == null) {
                // Not in dictionary... Add with value as null
                replicaState.ClientTable.Add(clientRequest.ClientId, new Tuple<int, ClientResponse>(-1, null));
            }
            
            lock (clientExecutor) {
                if (clientExecutor.AddedToQueue) {
                    Log.Debug($"Request #{clientExecutor.OpNumber} is scheduled to join Execution Queue.");
                    return;
                }

                clientExecutor.AddedToQueue = true;
            }
            // Add to execution queue
            replicaState.ExecutionQueue.Add(clientRequest, clientExecutor);
                     
        }
    }
}