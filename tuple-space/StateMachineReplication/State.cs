using MessageService;

namespace StateMachineReplication {
    public class NormalState : IState {
        public NormalState(SMRProtocol smrProtocol) {
            
        }

        public IResponse ProcessRequest(ISenderInformation info, IMessage message) {
            throw new System.NotImplementedException();
        }
    }
}