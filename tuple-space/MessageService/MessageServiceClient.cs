using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Threading.Tasks;

using log4net;

using MessageService.Serializable;

namespace MessageService {
    
    public class MessageServiceClient : IMessageServiceClient {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IMessageServiceClient));

        private readonly TcpChannel channel;

        public MessageServiceClient(Uri myUrl) {
            //create tcp channel
            this.channel = new TcpChannel(myUrl.Port);
            ChannelServices.RegisterChannel(this.channel, false);
            Log.Info($"TCP channel created at {myUrl.Host}:{myUrl.Port}.");
        }

        public MessageServiceClient(TcpChannel channel) {
            this.channel = channel;
        }

        public IResponse Request(IMessage message, Uri url) {
            Log.Debug($"Request called with parameters: message: {message}, url: {url}");
            MessageServiceServer server = GetRemoteMessageService(url);
            if (server != null) {
                return server.Request(message);
            }

            Log.Error($"Request: Could not resolve url {url.Host}:{url.Port}");
            return null;

        }

        public IResponse Request(IMessage message, Uri url, int timeout) {
            Log.Debug($"Request called with parameters: message: {message}, url: {url}, timeout: {timeout}");
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            
            // Create Task and run async
            Task<IResponse> task = Task<IResponse>.Factory.StartNew(() => {
                using (cancellationTokenSource.Token.Register(() => Log.Warn("Request: Task cancellation was issued."))) {
                    return this.Request(message, url);
                }
            }, cancellationTokenSource.Token);

            bool taskCompleted = timeout < 0 ? task.Wait(-1) : task.Wait(timeout);

            if (taskCompleted) {
                return task.Result;
            }

            // Cancel task, we don't care anymore.
            cancellationTokenSource.Cancel();
            Log.Error("Request: Timeout, abort thread request.");
            return null;
        }

        public IResponses RequestMulticast(
            IMessage message,
            Uri[] urls,
            int numberResponsesToWait,
            int timeout) {
            Log.Debug($"Multicast Request called with parameters: message: {message}, url: {urls},"
                      + $"numberResponsesToWait: {numberResponsesToWait}, timeout: {timeout}");

            if (numberResponsesToWait > urls.Length) {
                return null;
            } else if (numberResponsesToWait < 0) {
                numberResponsesToWait = urls.Length;
            }

            List<Task<IResponse>> tasks = new List<Task<IResponse>>();
            List<CancellationTokenSource> cancellations = new List<CancellationTokenSource>();

            foreach (Uri url in urls) {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                // Create Task and run async
                Task<IResponse> task = Task<IResponse>.Factory.StartNew(() => {
                    using (cancellationTokenSource.Token.Register(() => Log.Debug("Task cancellation requested"))) {
                        return this.Request(message, url);
                    }
                }, cancellationTokenSource.Token);

                tasks.Add(task);
                cancellations.Add(cancellationTokenSource);
            }

            // Wait until numberResponsesToWait
            CancellationTokenSource cancellationTs = new CancellationTokenSource();
            Task<IResponses> getResponses = Task<IResponses>.Factory.StartNew(
                () => { using (cancellationTs.Token.Register(() => { CancelSubTasks(cancellations); })) {
                        return GetResponses(numberResponsesToWait, tasks, cancellations);
                    }
                },
                cancellationTs.Token);

            bool taskCompleted = timeout < 0 ? getResponses.Wait(-1) : getResponses.Wait(timeout);

            if (taskCompleted) {
                Log.Debug($"Multicast response: {getResponses.Result}");
                return getResponses.Result;
            }

            // Cancel task, we don't care anymore.
            cancellationTs.Cancel();
            Log.Error("Multicast Request: Timeout, abort thread request.");
            return null;
        }

        private static IResponses GetResponses(int numberResponsesToWait, List<Task<IResponse>> tasks, List<CancellationTokenSource> cancellations) {
            IResponses responses = new Responses();
            int countMessages = 0;
            while (countMessages < numberResponsesToWait) {
                int index = Task.WaitAny(tasks.ToArray());

                responses.Add(tasks[index].Result);
                countMessages++;

                // cancel task
                cancellations[index].Cancel();

                // dispose task in the lists
                tasks.RemoveAt(index);
                cancellations.RemoveAt(index);
            }

            // cancel all other tasks
            foreach (CancellationTokenSource cancellationTokenSource in cancellations) {
                cancellationTokenSource.Cancel();
            }

            return responses;
        }

        private static void CancelSubTasks(List<CancellationTokenSource> cancellations) {
            Log.Warn("Multicast Request: cancellation was issued. Cancel all request Tasks.");
            // cancel all other tasks
            foreach (CancellationTokenSource cancellationTokenSource in cancellations) {
                cancellationTokenSource.Cancel();
            }
        }

        private static MessageServiceServer GetRemoteMessageService(Uri url) {
            string serviceUrl = $"tcp://{url.Host}:{url.Port}/{Constants.MESSAGE_SERVICE_NAME}";
            Log.Debug($"Activate service at {serviceUrl}");
            return (MessageServiceServer) Activator.GetObject(
                typeof(MessageServiceServer),
                serviceUrl);
        }

        public void Freeze() {
            throw new NotImplementedException();
        }

        public void Unfreeze() {
            throw new NotImplementedException();
        }
    }
}