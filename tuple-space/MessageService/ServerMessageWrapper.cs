using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using log4net;

namespace MessageService {
    public class ServerMessageWrapper {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServerMessageWrapper));

        private readonly TcpChannel channel;

        public MessageServiceServer ServiceServer { get; }

        public MessageServiceClient ServiceClient { get; }

        private readonly Uri url;
        private readonly int minDelay;
        private readonly int maxDelay;

        private bool frozen;

        public ServerMessageWrapper(Uri myUrl, IProtocol protocol, int minDelay, int maxDelay) {
            this.url = myUrl;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.frozen = false;

            // create tcp channel
            this.channel = new TcpChannel(myUrl.Port);
            ChannelServices.RegisterChannel(this.channel, false);
            Log.Info("TCP channel created.");

            // create MessageServiceServer
            this.ServiceServer = new MessageServiceServer(protocol, minDelay, maxDelay);

            // create MessageServiceClient
            this.ServiceClient = new MessageServiceClient(this.channel);
        }

        public string Status() {
            string status =
                $"Host: {url.Host} {Environment.NewLine}" +
                $"Port: {url.Port} {Environment.NewLine}" +
                $"Frozen: {frozen} {Environment.NewLine}" +
                $"MinDelay: {minDelay} {Environment.NewLine}" +
                $"MaxDelay: {maxDelay} {Environment.NewLine}";
            return status;
        }

        public void Freeze() {
            if (!frozen) {
                this.ServiceClient.Freeze();
                this.ServiceServer.Freeze();
                this.frozen = true;
            }
        }

        public void Unfreeze() {
            if (frozen) {
                this.ServiceClient.Unfreeze();
                this.ServiceServer.Unfreeze();
                this.frozen = false;
            }
        }
    }
}