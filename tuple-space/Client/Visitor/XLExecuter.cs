using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using log4net;

using Client.ScriptStructure;
using MessageService;
using MessageService.Serializable;
using Timeout = MessageService.Timeout;

namespace Client.Visitor {
    public class XLExecuter : IBasicVisitor {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SMRExecuter));
        private readonly MessageServiceClient messageServiceClient;
        private readonly Client client;

    public XLExecuter(MessageServiceClient messageServiceClient, Client client) {
            this.messageServiceClient = messageServiceClient;
            this.client = client;
        }

        public void VisitAdd(Add add) {
            AddRequest addRequest = new AddRequest(this.client.ViewNumber, this.client.Id, this.client.GetRequestNumber(), add.Tuple);
            this.RequestMulticast(addRequest);

            Console.WriteLine($"Added tuple {add.Tuple}");
        }
        
        public void VisitRead(Read read) {

            while (true) {
                ReadRequest readRequest = new ReadRequest(this.client.ViewNumber, this.client.Id, this.client.GetRequestNumber(), read.Tuple);

                IResponse[] responses = this.RequestMulticast(readRequest);

                foreach (IResponse response in responses) {
                    if (response != null && ((ClientResponse)response).Result != null) {
                        Console.WriteLine($"Read tuple = {((ClientResponse)response).Result}");
                        return;
                    }
                }

                Thread.Sleep(Timeout.TIMEOUT_XL_CLIENT_WAIT);
            }
            
        }

        public void VisitTake(Take take) {
            string tupleToTake;
            // First phase: Lock and choose a tuple from intersection
            while (true) {
                IResponse[] responses = this.GetAndLock(take);

                List<List<string>> intersection = new List<List<string>>();
                foreach (IResponse response in responses) {
                    if (response != null && ((GetAndLockResponse)response).Tuples.Count > 0) {
                        intersection.Add(((GetAndLockResponse)response).Tuples);
                    }
                }
                List<string> intersectTuples = ListUtils.IntersectLists(intersection);

                if (intersectTuples.Count <= 0) {
                    UnlockRequest unlockRequest = new UnlockRequest(
                        this.client.ViewNumber, 
                        this.client.Id, 
                        this.client.GetRequestNumber());
                    this.messageServiceClient.RequestMulticast(
                        unlockRequest, 
                        this.client.ViewServers, 
                        this.client.ViewServers.Length, 
                        -1, 
                        false);
                    Log.Debug("Take intersection is empty. Needs to be requested again.");
                    Thread.Sleep(Timeout.TIMEOUT_XL_CLIENT_WAIT);
                } else {
                    tupleToTake = intersectTuples[0];
                    break;
                }
            }
            
            // Second phase: Take
            TakeRequest takeRequest = new TakeRequest(
                this.client.ViewNumber,
                this.client.Id,
                this.client.GetRequestNumber(),
                tupleToTake);
            this.RequestMulticast(takeRequest);
            Console.WriteLine($"Take tuple = {tupleToTake}");
        }
        
        public void VisitRepeatBlock(RepeatBlock repeatBlock) {
            int numIterations = 0;

            while (numIterations < repeatBlock.NumRepeats) {
                foreach (BasicNode node in repeatBlock.Nodes) {
                    node.Accept(this);
                }

                numIterations++;
            }

        }

        public void VisitScript(Script script) {
            foreach (BasicNode node in script.Nodes) {
                node.Accept(this);
            }
        }
        
        public void VisitWait(Wait wait) {
            Thread.Sleep(wait.Time);
        }

        private IResponse[] RequestMulticast(ClientRequest clientRequest) {
            IResponse[] responses = { };
            while (responses.Length != this.client.ViewServers.Length) {
                responses = this.messageServiceClient.RequestMulticast(
                    clientRequest,
                    this.client.ViewServers,
                    this.client.ViewServers.Length,
                    Timeout.TIMEOUT_XL_CLIENT,
                    true).ToArray();

                if (responses.Length != this.client.ViewServers.Length) {
                    this.client.DoHandShake();
                    clientRequest.ViewNumber = this.client.ViewNumber;
                }
            }

            return responses;
        }

        private IResponse[] GetAndLock(Take take) {
            while (true) {
                GetAndLockRequest getAndLockRequest = new GetAndLockRequest(
                    this.client.ViewNumber,
                    this.client.Id,
                    this.client.GetRequestNumber(),
                    take.Tuple);
                IResponse[] responses = { };

                while (this.client.ViewServers.Length != responses.Length) {
                    int numberOfLockedRequests = this.client.ViewServers.Length;
                    responses = this.messageServiceClient.RequestMulticast(
                        getAndLockRequest,
                        this.client.ViewServers,
                        numberOfLockedRequests,
                        Timeout.TIMEOUT_XL_CLIENT,
                        true).ToArray();

                    if (responses.Length != this.client.ViewServers.Length) {
                        this.client.DoHandShake();
                        getAndLockRequest.ViewNumber = this.client.ViewNumber;
                    }

                    if (responses.Length == 0) {
                        continue;
                    }

                    // Check if refused
                    // No-one refused
                    if (responses.Length == numberOfLockedRequests) {
                        return responses;
                    }

                    // Some refused but the majority locked
                    if (responses.Length >= (numberOfLockedRequests / 2) + 1) {
                        continue;
                    }

                    // The majority didn't lock so it's needed to unlock
                    UnlockRequest unlockRequest = new UnlockRequest(
                        this.client.ViewNumber,
                        this.client.Id,
                        this.client.GetRequestNumber());

                    // Unlock
                    this.messageServiceClient.RequestMulticast(
                        unlockRequest,
                        this.client.ViewServers,
                        numberOfLockedRequests,
                        -1,
                        false).ToArray();
                    break;
                }

                return responses;
            }
        }
    }
}