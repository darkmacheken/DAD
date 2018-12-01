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

        private readonly ConcurrentDictionary<IMessage, AutoResetEvent> handlers;

        public MessageServiceServer(IProtocol protocol, int minDelay, int maxDelay) {
            this.protocol = protocol;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.frozen = false;
            this.handlers = new ConcurrentDictionary<IMessage, AutoResetEvent>();

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

            if (frozen) {
                AutoResetEvent myHandler = new AutoResetEvent(false);
                this.handlers.TryAdd(message, myHandler);
                while (frozen) {
                    myHandler.WaitOne();
                }
                this.handlers.TryRemove(message, out myHandler);
            }

            IResponse response = this.protocol.ProcessRequest(message);
            Log.Debug($"Response: {response}");
            return response;
        }

        public void Freeze() {
            this.frozen = true;
        }

        public void Unfreeze() {
            this.frozen = false;
            foreach (AutoResetEvent handler in this.handlers.Values) {
                handler.Set();
            }
        }

        public override object InitializeLifetimeService() {
            return null;
        }
    }
}