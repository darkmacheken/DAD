using System;
using Client.ScriptStructure;

namespace Client.Visitor
{
    public interface IBasicVisitor
    {
        void VisitAdd(Add add);
        void VisitRead(Read read);
        void VisitRepeatBlock(RepeatBlock repeatBlock);
        void VisitScript(Script script);
        void VisitTake(Take take);
        void VisitWait(Wait wait);

    }
}