using System;
using System.Threading;

using Client.ScriptStructure;

using log4net;

using MessageService;
using MessageService.Serializable;

namespace Client.Visitor {
    public class SMRExecuter : IBasicVisitor {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SMRExecuter));
        private readonly MessageServiceClient messageServiceClient;
        private readonly Client client;

        public SMRExecuter(MessageServiceClient messageServiceClient, Client client) {
            this.messageServiceClient = messageServiceClient;
            this.client = client;
        }

        public void VisitAdd(Add add) {
            ClientResponse clientResponse = (ClientResponse)this.messageServiceClient.Request(
                new AddRequest(this.client.Id, this.client.GetRequestNumber(), add.Tuple),
                new Uri("tcp://localhost:8080"));
            //TODO: the url cannot be hard coded.

            if (clientResponse != null) {
                Console.WriteLine($"Added tuple {add.Tuple}");
            } else {
                Log.Error("Add Request was outdated.");
            }
        }

        public void VisitRead(Read read) {
            ClientResponse clientResponse = (ClientResponse)this.messageServiceClient.Request(
                new ReadRequest(this.client.Id, this.client.GetRequestNumber(), read.Tuple),
                new Uri("tcp://localhost:8080"));
            //TODO: the url cannot be hard coded.

            if (clientResponse != null) {
                Console.WriteLine($"Read tuple = {clientResponse.Result}");
            } else {
                Log.Error("Read Request was outdated.");
            }
            
        }

        public void VisitTake(Take take) {
            ClientResponse clientResponse = (ClientResponse)this.messageServiceClient.Request(
                new TakeRequest(this.client.Id, this.client.GetRequestNumber(), take.Tuple),
                new Uri("tcp://localhost:8080"));
            //TODO: the url cannot be hard coded.

            if (clientResponse != null) {
                Console.WriteLine($"Take tuple = {clientResponse.Result}");
            } else {
                Log.Error("Take Request was outdated.");
            }
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