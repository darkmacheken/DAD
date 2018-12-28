using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class Exit : Command {

        public Exit() {}

        public override void Accept(IBasicVisitor visitor) {
            visitor.VisitExit(this);
        }

    }
}
