using Client.Visitor;

namespace Client.ScriptStructure {
    public class Wait : BasicNode {
        private readonly int time;

        public Wait(int time) {
            this.time = time;
        }

        public int Time => this.time;

        public override void Accept(IBasicVisitor v) {
            v.VisitWait(this);
        }
    }
}