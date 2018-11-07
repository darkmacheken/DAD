using System;

namespace MessageService {
    [Serializable]
    public class TestSenderInformation : ISenderInformation {
        public string Id { get; }

        public TestSenderInformation(string id) {
            this.Id = id;
        }
    }

    [Serializable]
    public class TestMessage : IMessage {
        public string Name { get; }

        public TestMessage(string name) {
            this.Name = name;
        }

    }

    [Serializable]
    public class TestResponse : IResponse {

        public string Response { get; }

        public TestResponse(string response) {
            this.Response = response;
        }
    }
}