using Client.Visitor;

namespace Client.ScriptStructure {
    public class Take : Command {
        public Take(string tuple) : base(tuple) {}

        public override void Accept(IBasicVisitor v) {
            v.VisitTake(this);
        }
    }
}