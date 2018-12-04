using System;
using System.Threading;
using System.Collections.Generic;

using log4net;

using Client.ScriptStructure;
using MessageService;
using MessageService.Serializable;

namespace Client.Visitor {
    public class XLExecuter : IBasicVisitor {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SMRExecuter));
        private readonly MessageServiceClient messageServiceClient;
        private readonly Client client;
        private readonly Uri[] replicasUrls;

    public XLExecuter(MessageServiceClient messageServiceClient, Client client) {
            this.messageServiceClient = messageServiceClient;
            this.client = client;
            this.replicasUrls = new[] {
                new Uri("tcp://localhost:8080"),
                new Uri("tcp://localhost:8081"), 
                new Uri("tcp://localhost:8082")};
        }

        public void VisitAdd(Add add) {
            AddRequest addRequest = new AddRequest(this.client.Id, this.client.GetRequestNumber(), add.Tuple);
            this.messageServiceClient.RequestMulticast(addRequest, this.replicasUrls, 3, -1, false);
            Console.WriteLine($"Added tuple {add.Tuple}");
        }

        public void VisitRead(Read read) {

            while (true) {
                ReadRequest readRequest = new ReadRequest(this.client.Id, this.client.GetRequestNumber(), read.Tuple);
                IResponses responses = this.messageServiceClient.RequestMulticast(readRequest, this.replicasUrls, 3, -1, false);

                foreach (IResponse response in responses.ToArray()) {
                    if (response != null && ((ClientResponse)response).Result != null) {
                        Console.WriteLine($"Read tuple = {((ClientResponse)response).Result}");
                        return;
                    }
                }
                Log.Debug("Read Request was outdated or needs to be requested again.");
                Thread.Sleep(100);
            }
            
        }

        public void VisitTake(Take take) {
            string tupleToTake;
            while (true) {
                GetAndLockRequest getAndLockRequest = new
                    GetAndLockRequest(this.client.ViewNumber, this.client.Id, this.client.GetRequestNumber(), take.Tuple);
                IResponses responses = this.messageServiceClient.RequestMulticast(getAndLockRequest, this.replicasUrls, 3, -1, false);
                List<List<string>> intersection = new List<List<string>>();
                foreach (IResponse response in responses.ToArray()) {
                    if (response != null && ((GetAndLockResponse)response).Tuples.Count > 0) {
                        intersection.Add(((GetAndLockResponse)response).Tuples);
                    }
                }
                List<string> intersectTuples = ListUtils.IntersectLists(intersection);
                if (intersectTuples.Count <= 0) {
                    UnlockRequest unlockRequest = new
                        UnlockRequest(this.client.ViewNumber, this.client.Id, this.client.GetRequestNumber(), this.client.GetRequestNumber());
                    this.messageServiceClient.RequestMulticast(unlockRequest, this.replicasUrls, 3, -1, false);
                    Log.Debug("Take intersection is empty. Needs to be requested again.");
                    Thread.Sleep(100);
                } else {
                    tupleToTake = intersectTuples[0];
                    break;
                }
            }
            
            TakeRequest takeRequest = new TakeRequest(
                this.client.Id,
                this.client.GetRequestNumber(),
                this.client.GetRequestNumber(), 
                tupleToTake);
            this.messageServiceClient.RequestMulticast(takeRequest, this.replicasUrls, 3, -1, false);
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
    }
}