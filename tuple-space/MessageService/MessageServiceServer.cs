using System;
using System.Collections.Concurrent;
using System.Runtime.Remoting;
using System.Threading;

using log4net;

namespace MessageService {
    public class MessageServiceServer : MarshalByRefObject, IMessageServiceServer {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IMessageServiceServer));

        private readonly IProtocol protocol;
        private readonly int minDelay;
        private readonly int maxDelay;

        private readonly Random seedRandom;

        private bool frozen;
        private int frozenRequests;

        private int IncrementFrozenRequests() { return Interlocked.Increment(ref this.frozenRequests); }
        private int DecrementFrozenRequests() { return Interlocked.Decrement(ref this.frozenRequests); }
        private readonly EventWaitHandle frozenRequestsHandler;

        private readonly EventWaitHandle handler;
        

        public MessageServiceServer(IProtocol protocol, int minDelay, int maxDelay) {
            this.protocol = protocol;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.frozen = false;
            this.handler = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.frozenRequestsHandler = new EventWaitHandle(false, EventResetMode.ManualReset);

            this.seedRandom = new Random();

            // Register remote
            RemotingServices.Marshal(
                this,
                Constants.MESSAGE_SERVICE_NAME,
                typeof(MessageServiceServer));
        }

        public IResponse Request(IMessage message) {
            int delay = this.seedRandom.Next(this.minDelay, this.maxDelay);

            Log.Debug($"Request (Process Delay = {delay} ms) with parameters: message: {message}");

            Thread.Sleep(delay);

            IResponse response = null;
            if (this.protocol.QueueWhenFrozen()) {
                if (frozen) {
                    this.IncrementFrozenRequests();
                    while (this.frozen) {
                        this.handler.WaitOne();
                    }

                    response = this.protocol.ProcessRequest(message);

                    this.DecrementFrozenRequests();
                    this.frozenRequestsHandler.Set();
                    this.frozenRequestsHandler.Reset();
                } else {
                    while (this.frozenRequests > 0) {
                        this.frozenRequestsHandler.WaitOne();
                    }

                    response = this.protocol.ProcessRequest(message);
                }
            } else {
                while (this.frozen) {
                    this.handler.WaitOne();
                }

                response = this.protocol.ProcessRequest(message);
            }
            
            Log.Debug($"Response: {response}");
            return response;
        }

        public void Freeze() {
            frozenRequests = 0;
            this.frozen = true;
        }

        public void Unfreeze() {
            this.frozen = false;
            this.handler.Set();
            this.handler.Reset();
        }

        public override object InitializeLifetimeService() {
            return null;
        }
    }
}