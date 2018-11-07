using System;
using Client.Visitor;

namespace Client.ScriptStructure {
    public class Script : Block {
        public Script() : base() {}

        public override void Accept(IBasicVisitor v) {
            v.VisitScript(this);
        }
    }
}