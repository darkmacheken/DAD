using Client.Visitor;

namespace Client.ScriptStructure
{
    public class Read : Command
    {
        public Read(string tuple) : base(tuple) {}

        public override void Accept(IBasicVisitor v)
        {
            v.VisitRead(this);
        }
    }
}