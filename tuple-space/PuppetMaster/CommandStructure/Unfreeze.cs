using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
   public class Unfreeze : DebuggingCommand {
        public Unfreeze(string processName) : base(processName) { }

        public override void Accept(IBasicVisitor visitor) {
            visitor.VisitUnfreeze(this);
        }
    }
}