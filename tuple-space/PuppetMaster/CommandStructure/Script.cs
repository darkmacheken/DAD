using System.Collections.Generic;
using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class Script : BasicNode {
        protected readonly List<Command> nodes;

        public Script() {
            this.nodes = new List<Command>();
        }

        public List<Command> Nodes => this.nodes;

        public void AddNode(Command node)
        {
            this.nodes.Add(node);
        }

        public override void Accept(IBasicVisitor v) {
            v.VisitScript(this);
        }
    }
}