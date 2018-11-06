using System;
using Client.ScriptStructure;

namespace Client.Visitor
{
    public class Writer : IBasicVisitor
    {
        public Writer()
        {
        }

        public void VisitAdd(Add add)
        {
            Console.WriteLine("add " + add.Tuple);
        }

        public void VisitRead(Read read)
        {
            Console.WriteLine("read " + read.Tuple);
        }

        public void VisitRepeatBlock(RepeatBlock repeatBlock)
        {
            Console.WriteLine("begin-repeat " + repeatBlock.NumRepeats);
            for (int i = 0; i < repeatBlock.Nodes.Count; i++)
            {
                repeatBlock.Nodes[i].Accept(this);
            }
            Console.WriteLine("end-repeat");
        }

        public void VisitScript(Script script)
        {
            Console.WriteLine("Script:\n");
            for (int i=0; i < script.Nodes.Count; i++)
            {
                script.Nodes[i].Accept(this);
            }
        }

        public void VisitTake(Take take)
        {
            Console.WriteLine("take " + take.Tuple);
        }

        public void VisitWait(Wait wait)
        {
            Console.WriteLine("wait " + wait.Time);
        }
    }
}
