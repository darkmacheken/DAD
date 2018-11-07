using MessageService.Messages;

namespace MessageService.Visitor {
    public interface ProcessRequestVisitor {
        void AcceptAddRequest(AddRequest addRequest, ISenderInformation info);

        void AcceptTakeRequest(TakeRequest takeRequest, ISenderInformation info);

        void AcceptReadRequest(ReadRequest readRequest, ISenderInformation info);
    }
}