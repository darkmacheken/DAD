namespace MessageService {
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Channels.Tcp;
    using System.Threading;

    using log4net;

    public delegate void AsyncRemoteRequestSendDelegate(ISenderInformation info, IMessage message);

    public class MessageService : IMessageService {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static string MessageServiceBasicName { get; } = "messageService";

        private readonly BlockingCollection<ReceivedRequest> receivedRequests;
        private readonly BlockingCollection<ReceivedMessage> receivedMessages;

        private readonly MessageServiceBasic messageServiceBasic; // act as a server

        private readonly TcpChannel channel;

        private readonly AutoResetEvent freezeHandle;
        private bool freeze;

        public MessageService(Uri myUrl, int minDelay, int maxDelay) {
            this.freeze = false;
            this.freezeHandle = new AutoResetEvent(false);

            this.receivedRequests = new BlockingCollection<ReceivedRequest>();
            this.receivedMessages = new BlockingCollection<ReceivedMessage>();
            this.messageServiceBasic = new MessageServiceBasic(
                this.receivedRequests, 
                this.receivedMessages,
                new Random().Next(minDelay, maxDelay));

            this.channel = new TcpChannel(myUrl.Port);
            ChannelServices.RegisterChannel(this.channel, false);
            Log.Info("TCP channel created.");

            //setup message basic service
            RemotingServices.Marshal(
                this.messageServiceBasic,
                MessageServiceBasicName,
                typeof(MessageServiceBasic));
            
            Log.Info("MessageServiceBasic running.");
        }

        public void Request(ISenderInformation info, IMessage message, Uri url) {
            Log.Debug($"Request called with parameters: info: {info}, message: {message}, url: {url}");
            MessageServiceBasic messageService = this.GetRemoteMessageService(url);
            if (messageService != null) {
                new AsyncRemoteRequestSendDelegate(messageService.Request).Invoke(info, message);
            } else {
                Log.Error($"Request: Could not resolve {url}.");
            }
        }

        public void Send(ISenderInformation info, IMessage message, Uri url) {
            Log.Debug($"Send called with parameters: info: {info}, message: {message}, url: {url}");
            MessageServiceBasic messageService = this.GetRemoteMessageService(url);
            if (messageService != null) {
                new AsyncRemoteRequestSendDelegate(messageService.Send).Invoke(info, message);
            } else {
                Log.Error($"Send: Could not resolve {url}.");
            }
        }

        public void RequestMulticast(ISenderInformation info, IMessage message, Uri[] urls) {
            Log.Debug($"Request called with parameters: info: {info}, message: {message}, url: {urls}");
            foreach (Uri url in urls) {
                MessageServiceBasic messageService = this.GetRemoteMessageService(url);
                if (messageService != null) {
                    new AsyncRemoteRequestSendDelegate(messageService.Request).Invoke(info, message);
                } else {
                    Log.Error($"RequestMulticast: Could not resolve {url}.");
                }
            }
        }

        public void SendMulticast(ISenderInformation info, IMessage message, Uri[] urls) {
            Log.Debug($"Request called with parameters: info: {info}, message: {message}, url: {urls}");
            foreach (Uri url in urls) {
                MessageServiceBasic messageService = this.GetRemoteMessageService(url);
                if (messageService != null) {
                    new AsyncRemoteRequestSendDelegate(messageService.Send).Invoke(info, message);
                } else {
                    Log.Error($"SendMulticast: Could not resolve {url}.");
                }
            }
        }

        public ReceivedMessage GetMessage() {
            while (this.freeze) {
                Log.Debug("Freeze is true, the GetMessage is about to block.");
                this.freezeHandle.WaitOne();
            }
            return this.receivedMessages.Take();
        }

        public ReceivedRequest GetRequest() {
            while (this.freeze) {
                Log.Debug("Freeze is true, the GetRequest is about to block.");
                this.freezeHandle.WaitOne();
            }
            return this.receivedRequests.Take();
        }

        public void Freeze() {
            lock (this) {
                this.freeze = true;
            }
            Log.Debug("Freeze is set to true.");
        }

        public void Unfreeze() {
            lock (this) {
                this.freeze = false;
                this.freezeHandle.Set();
            }
            Log.Debug("Freeze is set to false.");
        }

        private MessageServiceBasic GetRemoteMessageService(Uri url) {
            string serviceUrl = $"tcp://{url.Host}:{url.Port}/{MessageServiceBasicName}";
            Log.Debug($"Activate service at {serviceUrl}");
            return (MessageServiceBasic) Activator.GetObject(
                typeof(MessageServiceBasic),
                serviceUrl);
        }
    }

    public class MessageServiceBasic : MarshalByRefObject, IMessageServiceBasic {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public int Delay { get; }

        private readonly BlockingCollection<ReceivedRequest> receivedRequests;
        private readonly BlockingCollection<ReceivedMessage> receivedMessages;

        public MessageServiceBasic(BlockingCollection<ReceivedRequest> receivedRequests,
                                   BlockingCollection<ReceivedMessage> receivedMessages,
                                   int delay) {
            this.Delay = delay;
            this.receivedRequests = receivedRequests;
            this.receivedMessages = receivedMessages;
        }

        public void Request(ISenderInformation info, IMessage message) {
            Thread.Sleep(this.Delay);
            this.receivedRequests.Add(new ReceivedRequest(info, message));
        }

        public void Send(ISenderInformation info, IMessage message) {
            Thread.Sleep(this.Delay);
            this.receivedMessages.Add(new ReceivedMessage(info, message));
        }

    }

}