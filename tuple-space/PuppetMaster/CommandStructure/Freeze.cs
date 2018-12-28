using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class Freeze : DebuggingCommand {
        public Freeze(string processName) : base(processName) { }

        public override void Accept(IBasicVisitor visitor) {
            visitor.VisitFreeze(this);
        }
    }
}