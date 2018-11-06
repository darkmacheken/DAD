using Client.Visitor;

namespace Client.ScriptStructure
{
    public class Wait : BasicNode
    {
        private int time;

        public Wait(int time) : base() {
            this.time = time;
        }

        public int Time
        {
            get
            {
                return time;
            }
        }

        public override void Accept(IBasicVisitor v)
        {
            v.VisitWait(this);
        }
    }
}