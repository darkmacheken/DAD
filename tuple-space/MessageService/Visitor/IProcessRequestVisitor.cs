using MessageService.Messages;

namespace MessageService.Visitor {
    public interface IProcessRequestVisitor {
        IResponse AcceptAddRequest(AddRequest addRequest, ISenderInformation info);

        IResponse AcceptTakeRequest(TakeRequest takeRequest, ISenderInformation info);

        IResponse AcceptReadRequest(ReadRequest readRequest, ISenderInformation info);
    }
}