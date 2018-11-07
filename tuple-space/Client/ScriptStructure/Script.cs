using Client.Visitor;

namespace Client.ScriptStructure {
    public class Script : Block {
        public Script() {}

        public override void Accept(IBasicVisitor v) {
            v.VisitScript(this);
        }
    }
}