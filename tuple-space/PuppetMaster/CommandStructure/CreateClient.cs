using System;
using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class CreateClient : Command {
        private readonly string id;
        private readonly Uri url;
        private readonly string scriptFile;

        public CreateClient(string id, Uri url, string scriptFile) {
            this.id = id;
            this.url = url;
            this.scriptFile = scriptFile;
        }

        public override void Accept(IBasicVisitor visitor) {
            visitor.VisitCreateClient(this);
        }

        public string Id => this.id;

        public Uri Url => this.url;

        public string ScriptFile => this.scriptFile;

    }
}
