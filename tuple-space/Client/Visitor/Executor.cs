using System;
using System.Threading;

using Client.ScriptStructure;

using MessageService;
using MessageService.Messages;

namespace Client.Visitor {
    public class Executor : IBasicVisitor {
        private readonly MessageServiceClient messageServiceClient;
        private readonly Client client;

        public Executor(MessageServiceClient messageServiceClient, Client client) {
            this.messageServiceClient = messageServiceClient;
            this.client = client;
        }

        public void VisitAdd(Add add) {
            this.messageServiceClient.Request(
                new AddRequest(this.client.Id, this.client.GetRequestNumber(), add.Tuple),
                new Uri("tcp://localhost:8080"));
            //TODO: the url cannot be hard coded.
        }

        public void VisitRead(Read read) {
            this.messageServiceClient.Request(
                new ReadRequest(this.client.Id, this.client.GetRequestNumber(), read.Tuple),
                new Uri("tcp://localhost:8080"));
            //TODO: the url cannot be hard coded.
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

        public void VisitTake(Take take) {
            this.messageServiceClient.Request(
                new TakeRequest(this.client.Id, this.client.GetRequestNumber(), take.Tuple),
                new Uri("tcp://localhost:8080"));
            //TODO: the url cannot be hard coded.
        }

        public void VisitWait(Wait wait) {
            Thread.Sleep(wait.Time);
        }
    }
}