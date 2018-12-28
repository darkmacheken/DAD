using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public class Wait : Command {
        private readonly int time;

        public Wait(int time) {
            this.time = time;
        }

        public override void Accept(IBasicVisitor visitor)
        {
            visitor.VisitWait(this);
        }

        public int Time => this.time;
    }
}