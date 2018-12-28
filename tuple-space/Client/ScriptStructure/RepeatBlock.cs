using Client.Visitor;

namespace Client.ScriptStructure {
    public class RepeatBlock : Block {
        private readonly int numRepeats;

        public RepeatBlock(int numRepeats) {
            this.numRepeats = numRepeats;
        }

        public int NumRepeats => this.numRepeats;

        public override void Accept(IBasicVisitor v) {
            v.VisitRepeatBlock(this);
        }
    }
}