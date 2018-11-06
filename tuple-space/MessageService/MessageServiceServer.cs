using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

using log4net;

namespace MessageService {
    public class MessageServiceServer : MarshalByRefObject, IMessageServiceServer {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Protocol protocol;
        private readonly int minDelay;
        private readonly int maxDelay;

        private readonly Random seedRandom;


        public MessageServiceServer(Protocol protocol, int minDelay, int maxDelay) {
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

        public IResponse Request(ISenderInformation info, IMessage message) {
            int delay = this.seedRandom.Next(this.minDelay, this.maxDelay);

            Log.Debug($"Request (Process Delay = {delay} ms) with parameters: info: {info}, message: {message}");

            Thread.Sleep(delay);
            return this.protocol.ProcessRequest(info, message);
        }
    }
}