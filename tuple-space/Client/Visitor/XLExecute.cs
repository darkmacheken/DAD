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
            this.replicasUrls = new Uri[] {
                new Uri("tcp://localhost:8080"),
                new Uri("tcp://localhost:8081"), 
                new Uri("tcp://localhost:8082")};
        }

        public void VisitAdd(Add add) {
            AddRequest addRequest = new AddRequest(this.client.Id, this.client.GetRequestNumber(), add.Tuple);
            this.messageServiceClient.RequestMulticast(addRequest, this.replicasUrls, 3, -1);
            Console.WriteLine($"Added tuple {add.Tuple}");
        }

        public void VisitRead(Read read) {
            ReadRequest readRequest = new ReadRequest(this.client.Id, this.client.GetRequestNumber(), read.Tuple);
            IResponses responses = this.messageServiceClient.RequestMulticast(readRequest, this.replicasUrls, 3, -1);
            foreach(IResponse response in responses.ToArray()) {
                if (response!= null && !((ClientResponse)response).Result.Equals("null")) {
                    Console.WriteLine($"Read tuple = {((ClientResponse)response).Result}");
                    return;
                }
            }
            Log.Error("Read Request was outdated or needs to be requested again.");
        }

        public void VisitTake(Take take) {
            int requestNumberLock = this.client.GetRequestNumber();
            GetAndLockRequest getAndLockRequest = new 
                GetAndLockRequest(this.client.ViewId, this.client.Id, requestNumberLock, take.Tuple);
            IResponses responses = this.messageServiceClient.RequestMulticast(getAndLockRequest, this.replicasUrls, 3, -1);
            List<List<string>> intersection = new List<List<string>>();
            foreach (IResponse response in responses.ToArray()) {
                if (response != null && ((GetAndLockResponse)response).Tuples.Count > 0) {
                    intersection.Add(((GetAndLockResponse)response).Tuples);
                }
            }
            List<string> intersectTuples = ListUtils.IntersectLists(intersection);
            if(intersectTuples.Count <= 0) {
                UnlockRequest unlockRequest = new 
                    UnlockRequest(this.client.ViewId, this.client.Id, this.client.GetRequestNumber(), requestNumberLock);
                this.messageServiceClient.RequestMulticast(unlockRequest, this.replicasUrls, 3, -1);
                Log.Error("Take intersection is empty. Needs to be requested again.");
                return;
            }
            string tupleToTake = intersectTuples[0];


            TakeRequest takeRequest = new TakeRequest(
                this.client.Id,
                this.client.GetRequestNumber(), 
                requestNumberLock, 
                tupleToTake);
            this.messageServiceClient.RequestMulticast(takeRequest, this.replicasUrls, 3, -1);
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