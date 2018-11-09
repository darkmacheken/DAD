using System.Threading;

using Client.ScriptStructure;
using MessageService;

namespace Client.Visitor {
    public class XLExecuter : IBasicVisitor {
        private MessageServiceClient messageServiceClient;
        private Client client;

        public XLExecuter(MessageServiceClient messageServiceClient, Client client) {
            this.messageServiceClient = messageServiceClient;
            this.client = client;
        }

        public void VisitAdd(Add add) {
            throw new System.NotImplementedException();
        }

        public void VisitRead(Read read) {
            throw new System.NotImplementedException();
        }

        public void VisitTake(Take take) {
            throw new System.NotImplementedException();
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