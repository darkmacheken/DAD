using System.Collections.Generic;

namespace MessageService {
    public class Responses : IResponses {
        private readonly List<IResponse> responses;

        public Responses() {
            this.responses = new List<IResponse>();
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