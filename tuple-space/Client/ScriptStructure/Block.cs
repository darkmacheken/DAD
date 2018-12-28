using System.Collections.Generic;

namespace Client.ScriptStructure {
    public abstract class Block : BasicNode {
        protected readonly List<BasicNode> nodes;

        private readonly Parser parser;

        public Block() {
            this.nodes = new List<BasicNode>();
            this.parser = new Parser();
        }

        public List<BasicNode> Nodes => this.nodes;

        public void AddNode(BasicNode node) {
            this.nodes.Add(node);
        }

        public int Parse(Parser parser, string[] lines, int index) {
            return parser.Parse(lines, this, index);
        }
    }
}