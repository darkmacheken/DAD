using System;
using Client.Visitor;

namespace Client.ScriptStructure {
    public class RepeatBlock : Block {
        private int numRepeats;

        public RepeatBlock(int numRepeats) : base()  {
            this.numRepeats = numRepeats;
        }

        public int NumRepeats {
            get {
                return numRepeats;
            }
        }

        public override void Accept(IBasicVisitor v) {
            v.VisitRepeatBlock(this);
        }
    }
}