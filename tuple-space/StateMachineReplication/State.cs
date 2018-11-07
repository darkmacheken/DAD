using MessageService;

namespace StateMachineReplication {
    public class NormalState : IState {
        public NormalState(SMRProtocol smrProtocol) {
            throw new System.NotImplementedException();
        }

        public IResponse ProcessRequest(ISenderInformation info, IMessage message) {
            throw new System.NotImplementedException();
        }
    }
}