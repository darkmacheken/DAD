using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class Crash : DebuggingCommand {
        public Crash(string processName) : base(processName) { }

        public override void Accept(IBasicVisitor v) {
            v.VisitCrash(this);
        }
    }
}