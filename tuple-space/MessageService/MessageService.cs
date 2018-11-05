namespace MessageService {
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Channels.Tcp;
    using System.Threading;

    public delegate void AsyncRemoteRequestSendDelegate(ISenderInformation info, IMessage message);

    public class MessageService : IMessageService {
        private static readonly string messageServiceBasicName = "messageService";
        private readonly BlockingCollection<ReceivedRequest> receivedRequests;
        private readonly BlockingCollection<ReceivedMessage> receivedMessages;

        private readonly MessageServiceBasic messageServiceBasic; // act as a server

        private readonly TcpChannel channel;

        public MessageService(Uri myUrl, int minDelay, int maxDelay) {
            this.receivedRequests = new BlockingCollection<ReceivedRequest>();
            this.receivedMessages = new BlockingCollection<ReceivedMessage>();
            this.messageServiceBasic = new MessageServiceBasic(
                this.receivedRequests, 
                this.receivedMessages,
                new Random().Next(minDelay, maxDelay));

            this.channel = new TcpChannel(myUrl.Port);
            ChannelServices.RegisterChannel(this.channel, false);

            //setup message basic service
            RemotingServices.Marshal(
                this.messageServiceBasic,
                messageServiceBasicName,
                typeof(MessageServiceBasic));

        }

        public void Request(ISenderInformation info, IMessage message, Uri url) {
            MessageServiceBasic messageService = this.GetRemoteMessageService(url);
            if (messageService != null) {
                new AsyncRemoteRequestSendDelegate(messageService.Request).Invoke(info, message);
            } else {
                Console.WriteLine($"Could not resolve {url}.");
            }
        }

        public void Send(ISenderInformation info, IMessage message, Uri url) {
            MessageServiceBasic messageService = this.GetRemoteMessageService(url);
            if (messageService != null) {
                new AsyncRemoteRequestSendDelegate(messageService.Send).Invoke(info, message);
            } else {
                Console.WriteLine($"Could not resolve {url}.");
            }
        }

        public void RequestMulticast(ISenderInformation info, IMessage message, Uri[] urls) {
            foreach (Uri url in urls) {
                MessageServiceBasic messageService = this.GetRemoteMessageService(url);
                if (messageService != null) {
                    new AsyncRemoteRequestSendDelegate(messageService.Request).Invoke(info, message);
                } else {
                    Console.WriteLine($"Could not resolve {url}.");
                }
            }
        }

        public void SendMulticast(ISenderInformation info, IMessage message, Uri[] urls) {
            foreach (Uri url in urls) {
                MessageServiceBasic messageService = this.GetRemoteMessageService(url);
                if (messageService != null) {
                    new AsyncRemoteRequestSendDelegate(messageService.Send).Invoke(info, message);
                } else {
                    Console.WriteLine($"Could not resolve {url}.");
                }
            }
        }

        private MessageServiceBasic GetRemoteMessageService(Uri url) {
            return (MessageServiceBasic) Activator.GetObject(
                typeof(MessageServiceBasic),
                $"tcp://{url.Host}:{url.Port}/{messageServiceBasicName}");
        }
    }

    public class MessageServiceBasic : MarshalByRefObject, IMessageServiceBasic {
        private readonly int delay;
        private readonly BlockingCollection<ReceivedRequest> receivedRequests;
        private readonly BlockingCollection<ReceivedMessage> receivedMessages;

        public MessageServiceBasic(BlockingCollection<ReceivedRequest> receivedRequests,
                                   BlockingCollection<ReceivedMessage> receivedMessages,
                                   int delay) {
            this.delay = delay;
            this.receivedRequests = receivedRequests;
            this.receivedMessages = receivedMessages;
        }

        public void Request(ISenderInformation info, IMessage message) {
            Thread.Sleep(this.delay);
            this.receivedRequests.Add(new ReceivedRequest(info, message));
        }

        public void Send(ISenderInformation info, IMessage message) {
            Thread.Sleep(this.delay);
            this.receivedMessages.Add(new ReceivedMessage(info, message));
        }

    }

}