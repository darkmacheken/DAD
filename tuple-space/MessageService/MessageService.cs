using System;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using log4net;

namespace MessageService {
    public class MessageService {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TcpChannel channel;

        private readonly MessageServiceServer messageServiceServer;
        private readonly MessageServiceClient messageServiceClient;

        public MessageService(Uri myUrl, IProtocol protocol, int minDelay, int maxDelay) {
            // create tcp channel
            this.channel = new TcpChannel(myUrl.Port);
            ChannelServices.RegisterChannel(this.channel, false);
            Log.Info("TCP channel created.");

            // create MessageServiceServer
            this.messageServiceServer = new MessageServiceServer(protocol, minDelay, maxDelay);

            // create MessageServiceClient
            this.messageServiceClient = new MessageServiceClient(this.channel);
        }

        public void Freeze() {
            this.messageServiceClient.Freeze();
            this.messageServiceServer.Freeze();
        }

        public void Unfreeze() {
            this.messageServiceClient.Unfreeze();
            this.messageServiceServer.Unfreeze();
        }
    }
}