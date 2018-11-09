using System;
using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class CreateServer : Command {
        private readonly string id;
        private readonly Uri url;
        private readonly int minDelay;
        private readonly int maxDelay;

        public CreateServer(string id, Uri url, int minDelay, int maxDelay) {
            this.id = id;
            this.url = url;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
        }

        public override void Accept(IBasicVisitor v) {
            v.VisitCreateServer(this);
        }

        public string Id => this.id;

        public Uri Url => this.url;

        public int MinDelay => this.minDelay;

        public int MaxDelay => this.maxDelay;
    }
}
