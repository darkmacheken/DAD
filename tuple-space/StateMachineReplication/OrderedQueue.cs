using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using MessageService.Serializable;

namespace StateMachineReplication {
    public class OrderedQueue {
        private readonly BlockingCollection<Executor> requestsToExecute;
        private readonly ConcurrentDictionary<string,AutoResetEvent> handlers;
        private int lastOpNumber;

        public OrderedQueue() {
            requestsToExecute = new BlockingCollection<Executor>();
            handlers = new ConcurrentDictionary<string, AutoResetEvent>();
            lastOpNumber = 0;
        }
        
        /// <summary>
        /// Adds an element in the queue. It BLOCKS while the op number of the element isn't the
        /// next op number of the last added element.
        /// </summary>
        /// <param name="requestToExecute">The element to add.</param>
        public void Add(Executor requestToExecute) {
            AutoResetEvent myHandler = new AutoResetEvent(false);

            handlers.TryAdd(requestToExecute.ClientId, myHandler);

            while (lastOpNumber != requestToExecute.OpNumber - 1) {
                myHandler.WaitOne();
            }

            requestsToExecute.Add(requestToExecute);
            this.lastOpNumber = requestToExecute.OpNumber;

            // notify all waiting threads
            foreach (AutoResetEvent eventHandler in this.handlers.Values) {
                eventHandler.Set();
            }
            this.handlers.TryRemove(requestToExecute.ClientId, out myHandler);
        }

        /// <summary>
        /// Takes the element in the head of the queue. It BLOCKS until there is an element
        /// in the queue.
        /// </summary>
        /// <returns>The element in the top of the queue</returns>
        public Executor Take() {
            return this.requestsToExecute.Take();
        }
    }
}