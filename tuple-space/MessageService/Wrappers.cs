namespace MessageService {
    public class ReceivedRequest {
        public ISenderInformation Info { get; }
        public IMessage Message { get; }

        public ReceivedRequest(ISenderInformation info, IMessage message) {
            this.Info = info;
            this.Message = message;
        }
    }

    public class ReceivedMessage {
        public ISenderInformation Info { get; }
        public IMessage Message { get; }

        public ReceivedMessage(ISenderInformation info, IMessage message) {
            this.Info = info;
            this.Message = message;
        }
    }
}