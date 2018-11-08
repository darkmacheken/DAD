using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

using log4net;

namespace MessageService {
    public class MessageServiceServer : MarshalByRefObject, IMessageServiceServer {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(IMessageServiceServer));

        private readonly IProtocol protocol;
        private readonly int minDelay;
        private readonly int maxDelay;

        private readonly Random seedRandom;


        public MessageServiceServer(IProtocol protocol, int minDelay, int maxDelay) {
            this.protocol = protocol;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;

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
            IResponse response = this.protocol.ProcessRequest(message);
            Log.Debug($"Response: {response}");
            return response;
        }

        public void Freeze() {
            throw new NotImplementedException();
        }

        public void Unfreeze() {
            throw new NotImplementedException();
        }
    }
}