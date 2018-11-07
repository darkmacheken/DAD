using Client.Visitor;

namespace Client.ScriptStructure {
    public abstract class BasicNode {
        public BasicNode() {}

        public abstract void Accept(IBasicVisitor v);
    }
}
