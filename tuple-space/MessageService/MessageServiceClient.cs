using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace MessageService {
    
    public class MessageServiceClient : IMessageServiceClient {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TcpChannel channel;

        public MessageServiceClient(Uri myUrl) {
            //create tcp channel
            this.channel = new TcpChannel(myUrl.Port);
            ChannelServices.RegisterChannel(this.channel, false);
            Log.Info("TCP channel created.");
        }

        public MessageServiceClient(TcpChannel channel) {
            this.channel = channel;
        }

        public IResponse Request(ISenderInformation info, IMessage message, Uri url) {
            Log.Debug($"Request called with parameters: info: {info}, message: {message}, url: {url}");
            MessageServiceServer server = GetRemoteMessageService(url);
            if (server != null) {
                return server.Request(info, message);
            }

            Log.Error($"Request: Could not resolve url {url}");
            return null;

        }

        public IResponse Request(ISenderInformation info, IMessage message, Uri url, int timeout) {
            Log.Debug($"Request called with parameters: info: {info}, message: {message}, url: {url}, timeout: {timeout}");
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            
            // Create Task and run async
            Task<IResponse> task = Task<IResponse>.Factory.StartNew(() => {
                using (cancellationTokenSource.Token.Register(() => Log.Debug("Task cancellation requested"))) {
                    return this.Request(info, message, url);
                }
            }, cancellationTokenSource.Token);

            bool taskCompleted = timeout < 0 ? task.Wait(-1) : task.Wait(timeout);

            if (taskCompleted) {
                return task.Result;
            }

            // Cancel task, we don't care anymore.
            cancellationTokenSource.Cancel();
            Log.Error($"Request: Timeout, abort thread request.");
            return null;
        }

        public IResponses RequestMulticast(
            ISenderInformation info,
            IMessage message,
            Uri[] urls,
            int numberResponsesToWait,
            int timeout) {
            Log.Debug($"Request called with parameters: info: {info}, message: {message}, url: {urls},"
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
                        return this.Request(info, message, url);
                    }
                }, cancellationTokenSource.Token);

                tasks.Add(task);
                cancellations.Add(cancellationTokenSource);
            }

            // Wait until numberResponsesToWait
            CancellationTokenSource cancellationTS = new CancellationTokenSource();
            Task<IResponses> getRequests = Task<IResponses>.Factory.StartNew(
                () => {
                    using (
                        cancellationTS.Token.Register(() => Log.Debug("Task cancellation requested"))) {
                        IResponses responses = new Responses();
                        int countMessages = 0;
                        while (countMessages != numberResponsesToWait) {
                            int index = timeout < 0
                                            ? Task.WaitAny(tasks.ToArray(), -1)
                                            : Task.WaitAny(tasks.ToArray(), timeout);

                            if (index == -1) {
                                // timeout
                                return null;
                            }

                            responses.Add(tasks[index].Result);

                            // cancel task
                            cancellations[index].Cancel();

                            // dispose task in the lists
                            tasks.RemoveAt(index);
                            cancellations.RemoveAt(index);
                        }

                        return responses;
                    }
                },
                cancellationTS.Token);

            bool taskCompleted = timeout < 0 ? getRequests.Wait(-1) : getRequests.Wait(timeout);

            if (taskCompleted) {
                return getRequests.Result;
            }

            // Cancel task, we don't care anymore.
            cancellationTS.Cancel();
            Log.Error($"Request: Timeout, abort thread request.");
            return null;
        }

        private static MessageServiceServer GetRemoteMessageService(Uri url) {
            string serviceUrl = $"tcp://{url.Host}:{url.Port}/{Constants.MESSAGE_SERVICE_NAME}";
            Log.Debug($"Activate service at {serviceUrl}");
            return (MessageServiceServer) Activator.GetObject(
                typeof(MessageServiceServer),
                serviceUrl);
        }
    }
}