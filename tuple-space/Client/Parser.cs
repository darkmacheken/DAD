using System;
using Client.ScriptStructure;

namespace Client
{
    public class Parser
    {
       
        public int Parse(string[] lines, Block block, int i) 
        {
            for (; i < lines.Length; i++) 
            {
                string[] words = lines[i].Split(' ');

                switch (words[0])
                {
                    case "add":
                        block.AddNode(new Add(words[1]));
                        break;

                    case "read":
                        block.AddNode(new Read(words[1]));
                        break;

                    case "take":
                        block.AddNode(new Take(words[1]));
                        break;

                    case "wait":
                        block.AddNode(new Wait(Int32.Parse(words[1])));
                        break;

                    case "begin-repeat":
                        if (block is Script)
                        {
                            RepeatBlock rb = new RepeatBlock(Int32.Parse(words[1]));
                            i = rb.Parse(this, lines, i + 1);
                            block.AddNode(rb);
                        }
                        break;

                    case "end-repeat":
                        if (block is RepeatBlock)
                        {
                            return i;
                        }
                        break;
                }
            }
            return i;
        }
    }
}