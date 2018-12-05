using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Threading.Tasks;

using log4net;

namespace MessageService {
    
    public class MessageServiceClient : IMessageServiceClient {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IMessageServiceClient));

        private readonly TcpChannel channel;

        private bool frozen;
        private EventWaitHandle freezeHandler;

        public MessageServiceClient(Uri myUrl) {
            this.frozen = false;
            this.freezeHandler = new EventWaitHandle(false, EventResetMode.ManualReset);

            //create tcp channel
            this.channel = new TcpChannel(myUrl.Port);
            ChannelServices.RegisterChannel(this.channel, false);
            Log.Info($"TCP channel created at {myUrl.Host}:{myUrl.Port}.");
        }

        public MessageServiceClient(TcpChannel channel) {
            this.frozen = false;
            this.freezeHandler = new EventWaitHandle(false, EventResetMode.ManualReset);

            this.channel = channel;
        }

        public IResponse Request(IMessage message, Uri url) {
            // block if frozen
            this.BlockFreezeState(message);
            try {
                Log.Debug($"Request called with parameters: message: {message}, url: {url}");
                MessageServiceServer server = GetRemoteMessageService(url);
                if (server != null) {
                    return server.Request(message);
                }

                Log.Error($"Request: Could not resolve url {url.Host}:{url.Port}");
                return null;
            } catch (Exception) {
                return null;
            }
        }

       public IResponse Request(IMessage message, Uri url, int timeout) {
           // block if frozen
           this.BlockFreezeState(message);

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
            int timeout,
            bool notNull) {
            // block if frozen
            this.BlockFreezeState(message);

            if (urls.Length == 0) {
                return new Responses();
            }

            Log.Debug($"Multicast Request called with parameters: message: {message}, url: {urls},"
                      + $"numberResponsesToWait: {numberResponsesToWait}, timeout: {timeout}");

            if (numberResponsesToWait > urls.Length) {
                return new Responses();
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

                lock (tasks) {
                    tasks.Add(task);
                }

                lock (cancellations) {
                    cancellations.Add(cancellationTokenSource);
                }
                
            }

            // Wait until numberResponsesToWait
            IResponses responses = new Responses();
            CancellationTokenSource cancellationTs = new CancellationTokenSource();
            Task getResponses = Task.Factory.StartNew(
                () => { using (cancellationTs.Token.Register(() => { MessageServiceClient.CancelSubTasks(cancellations); })) {
                        GetResponses(responses, numberResponsesToWait, tasks, cancellations, notNull);
                    }
                },
                cancellationTs.Token);

            bool taskCompleted = timeout < 0 ? getResponses.Wait(-1) : getResponses.Wait(timeout);

            if (taskCompleted) {
                Log.Debug($"Multicast response: {responses}");
                return responses;
            }

            // Cancel task, we don't care anymore.
            cancellationTs.Cancel();
            Log.Error("Multicast Request: Timeout, abort thread request.");
            return responses;
        }

        private static void GetResponses(
            IResponses responses,
            int numberResponsesToWait, 
            List<Task<IResponse>> tasks, 
            List<CancellationTokenSource> cancellations,
            bool notNull) {

            int countMessages = 0;
            while (countMessages < numberResponsesToWait) {
                int index = Task.WaitAny(tasks.ToArray());
                if (index < 0) {
                    return;
                }
                if (notNull) {
                    if (tasks[index].Result != null) {
                        lock (responses) {
                            responses.Add(tasks[index].Result);
                            countMessages++;
                        }
                    }
                } else {
                    lock (responses) {
                        responses.Add(tasks[index].Result);
                        countMessages++;
                    }
                }

                lock (cancellations) {
                    // cancel task
                    cancellations[index].Cancel();
                    cancellations.RemoveAt(index);
                }

                lock (tasks) {
                    tasks.RemoveAt(index);
                }
            }

            // Cancel remaining sub-tasks
            MessageServiceClient.CancelSubTasks(cancellations);
        }

        private static void CancelSubTasks(List<CancellationTokenSource> cancellations) {
            Log.Warn("Multicast Request: cancellation was issued. Cancel all request Tasks.");
            // cancel all other tasks
            lock (cancellations) {
                foreach (CancellationTokenSource cancellationTokenSource in cancellations) {
                    cancellationTokenSource.Cancel();
                }
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
            this.frozen = true;
        }

        public void Unfreeze() {
            this.frozen = false;
            this.freezeHandler.Set();
            this.freezeHandler.Reset();
        }

        private void BlockFreezeState(IMessage message) {
            while (frozen) {
                this.freezeHandler.WaitOne();
            }
        }
    }
}