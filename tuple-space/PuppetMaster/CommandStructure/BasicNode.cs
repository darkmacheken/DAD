using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public abstract class BasicNode {
        public BasicNode() {}

        public abstract void Accept(IBasicVisitor visitor);
    }
}
