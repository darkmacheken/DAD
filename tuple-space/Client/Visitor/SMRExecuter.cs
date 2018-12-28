using System;
using System.Threading;

using Client.ScriptStructure;

using log4net;

using MessageService;
using MessageService.Serializable;
using Timeout = MessageService.Timeout;

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
            AddRequest addRequest = new AddRequest(this.client.Id, this.client.GetRequestNumber(), add.Tuple);

            ClientResponse clientResponse = null;

            while (clientResponse == null) {
                clientResponse = (ClientResponse)this.messageServiceClient.Request(
                    addRequest,
                    this.client.Leader,
                    Timeout.TIMEOUT_SMR_CLIENT);

                if (clientResponse != null) {
                    Console.WriteLine($"Added tuple {add.Tuple}");
                    break;
                }

                this.client.DoHandShake();
            }
        }

        public void VisitRead(Read read) {
            ClientResponse clientResponse;
            do {
                clientResponse = null;
                ReadRequest readRequest = new ReadRequest(this.client.Id, this.client.GetRequestNumber(), read.Tuple);
                while (clientResponse == null) {
                    clientResponse = (ClientResponse)this.messageServiceClient.Request(
                        readRequest,
                        this.client.Leader,
                        Timeout.TIMEOUT_SMR_CLIENT);
                    if (clientResponse != null) {
                        break;
                    }

                    this.client.DoHandShake();
                }

                if (clientResponse.Result == null) {
                    Thread.Sleep(Timeout.TIMEOUT_SMR_CLIENT_WAIT);
                }
            } while (clientResponse.Result == null);

            Console.WriteLine($"Read tuple = {clientResponse.Result}");

        }

        public void VisitTake(Take take) {
            ClientResponse clientResponse;
            do {
                clientResponse = null;
                TakeRequest readRequest = new TakeRequest(this.client.Id, this.client.GetRequestNumber(), take.Tuple);
                while (clientResponse == null) {
                    clientResponse = (ClientResponse)this.messageServiceClient.Request(
                        readRequest,
                        this.client.Leader,
                        Timeout.TIMEOUT_SMR_CLIENT);
                    if (clientResponse != null) {
                        break;
                    }

                    this.client.DoHandShake();
                }

                if (clientResponse.Result == null) {
                    
                    Thread.Sleep(Timeout.TIMEOUT_SMR_CLIENT_WAIT);
                }
            } while (clientResponse.Result == null);

            Console.WriteLine($"Take tuple = {clientResponse.Result}");
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