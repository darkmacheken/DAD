using Client.Visitor;

namespace Client.ScriptStructure
{
    public class Add : Command
    {
        public Add(string tuple) : base(tuple) {}

        public override void Accept(IBasicVisitor v)
        {
            v.VisitAdd(this);
        }
    }
}