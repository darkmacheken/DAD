using System;
using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class CreateServer : Command {
        public string Id { get; }
        public Uri Url { get; }
        public int MinDelay { get; }
        public int MaxDelay { get; }
        public string Protocol { get; }

        public CreateServer(string id, Uri url, int minDelay, int maxDelay, string protocol) {
            this.Id = id;
            this.Url = url;
            this.MinDelay = minDelay;
            this.MaxDelay = maxDelay;
            this.Protocol = protocol;
        }

        public override void Accept(IBasicVisitor visitor) {
            visitor.VisitCreateServer(this);
        }
    }
}
