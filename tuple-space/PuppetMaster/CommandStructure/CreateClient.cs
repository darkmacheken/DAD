using System;
using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class CreateClient : Command {
        public string Id { get; }
        public Uri Url { get; }
        public string ScriptFile { get; }

        public CreateClient(string id, Uri url, string scriptFile) {
            this.Id = id;
            this.Url = new Uri($"tcp://{url.Host}:{url.Port}");
            this.ScriptFile = scriptFile;
        }

        public override void Accept(IBasicVisitor visitor) {
            visitor.VisitCreateClient(this);
        }
    }
}
