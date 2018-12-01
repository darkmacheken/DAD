using System.Collections.Concurrent;

namespace MessageService {
    public class Responses : IResponses {
        private readonly ConcurrentBag<IResponse> responses;

        public Responses() {
            this.responses = new ConcurrentBag<IResponse>();
        }

        public void Add(IResponse response) {
            this.responses.Add(response);
        }

        public IResponse[] ToArray() {
            return this.responses.ToArray();
        }

        public int Count() {
            return this.responses.Count;
        }
    }
}