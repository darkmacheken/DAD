using System;
using System.Diagnostics;

using MessageService;
using MessageService.Messages;
using MessageService.Visitor;

namespace StateMachineReplication.StateProcess {
    public class NormalStateProcessRequest : IProcessRequestVisitor {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(NormalStateProcessRequest));
        private readonly ReplicaState replicaState;

        public NormalStateProcessRequest(ReplicaState replicaState) {
            this.replicaState = replicaState;
        }

        public IResponse AcceptAddRequest(AddRequest addRequest, ISenderInformation info) {
            // TODO: check info type
            ClientInfo clientInfo = (ClientInfo)info;
            Console.WriteLine($"Processing Add request with tuple {addRequest.Tuple} from client {clientInfo.ClientId}");
            return null;
        }

        public IResponse AcceptTakeRequest(TakeRequest takeRequest, ISenderInformation info) {
            // TODO: check info type
            ClientInfo clientInfo = (ClientInfo)info;
            Console.WriteLine($"Processing Take request with tuple {takeRequest.Tuple} from client {clientInfo.ClientId}");
            return null;
        }

        public IResponse AcceptReadRequest(ReadRequest readRequest, ISenderInformation info) {
            // TODO: check info type
            ClientInfo clientInfo = (ClientInfo)info;
            Console.WriteLine($"Processing Read request with tuple {readRequest.Tuple} from client {clientInfo.ClientId}");
            return null;
        }
    }
}