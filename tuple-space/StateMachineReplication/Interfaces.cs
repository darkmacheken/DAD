using MessageService;

namespace StateMachineReplication {
    public interface IState {
        IResponse ProcessRequest(ISenderInformation info, IMessage message);
    }
}