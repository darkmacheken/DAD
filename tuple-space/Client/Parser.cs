using System;
using System.Text.RegularExpressions;
using Client.Exceptions;
using Client.ScriptStructure;

namespace Client {
    public class Parser {
        //original regex expr: < *((("((\*?[a-z]*)|([a-z]*\*?))")|(null)|([A-Za-z_\-]+(\( *((([0-9]+)|("([a-z]*)")) *, *)*((([0-9]+)|("([a-z]*)")) *)\))?)) *, *)*(("((\*?[a-z]*)|([a-z]*\*?))")|(null)|([A-Za-z_\-]+(\( *((([0-9]+)|("([a-z]*)")) *, *)*((([0-9]+)|("([a-z]*)")) *)\))?)) *>
        private string tupleExpr = "^< *(((\"((\\*?[a-z]*)|([a-z]*\\*?))\")|(null)" +
        	"|([A-Za-z_\\-]+(\\( *((([0-9]+)|(\"([a-z]*)\")) *, *)*((([0-9]+)|(\"(" +
        	"[a-z]*)\")) *)\\))?)) *, *)*((\"((\\*?[a-z]*)|([a-z]*\\*?))\")|(null)" +
        	"|([A-Za-z_\\-]+(\\( *((([0-9]+)|(\"([a-z]*)\")) *, *)*((([0-9]+)|(\"(" +
        	"[a-z]*)\")) *)\\))?)) *>$";

        public int Parse(string[] lines, Block block, int i) {
            Regex tupleRegex = new Regex(this.tupleExpr);
            Regex numRegex = new Regex("^[0-9]+$");

            for (; i < lines.Length; i++) {

                if (lines[i].StartsWith("\n") || lines[i].StartsWith("\r\n") || string.IsNullOrWhiteSpace(lines[i])) {
                    continue;
                }
                if (lines[i].StartsWith(" ") || lines[i].StartsWith("\t")) {
                    throw new IncorrectCommandException(i);
                }

                Regex exprRegex = new Regex("(\\s|\\n|\\t|\\r)+");
                string command = exprRegex.Replace(lines[i], string.Empty);

                if (command.StartsWith("add")) {
                    exprRegex = new Regex("add");
                    string argument = exprRegex.Replace(command, string.Empty, 1);
                    if (!tupleRegex.IsMatch(argument)) {
                        throw new IncorrectCommandException(i);
                    }
                    block.AddNode(new Add(argument));

                } else if (command.StartsWith("read")) {
                    exprRegex = new Regex("read");
                    string argument = exprRegex.Replace(command, string.Empty, 1);
                    if (!tupleRegex.IsMatch(argument)) {
                        throw new IncorrectCommandException(i);
                    }
                    block.AddNode(new Read(argument));

                } else if (command.StartsWith("take")) {
                    exprRegex = new Regex("take");
                    string argument = exprRegex.Replace(command, string.Empty, 1);
                    if (!tupleRegex.IsMatch(argument)) {
                        throw new IncorrectCommandException(i);
                    }
                    block.AddNode(new Take(argument));

                } else if (command.StartsWith("wait")) {
                    exprRegex = new Regex("wait");
                    string argument = exprRegex.Replace(command, string.Empty, 1);
                    if (!numRegex.IsMatch(argument)) {
                        throw new IncorrectCommandException(i);
                    }
                    int num = Int32.Parse(argument);
                    block.AddNode(new Wait(num));
                
                } else if (command.StartsWith("begin-repeat")) {
                    if (!(block is Script)) {
                        throw new IncorrectCommandException(i);
                    }
                    exprRegex = new Regex("begin-repeat");
                    string argument = exprRegex.Replace(command, string.Empty, 1);
                    if (!numRegex.IsMatch(argument)) {
                        throw new IncorrectCommandException(i);
                    }
                    int num = Int32.Parse(argument);
                   
                    RepeatBlock rb = new RepeatBlock(num);
                    i = rb.Parse(this, lines, i + 1);
                    block.AddNode(rb);

                    //verify if it reached end (and the loop wasn't closed)
                    if (i == lines.Length) {
                        throw new BlockEndMissingException(i);
                    }
                
                } else if (command.StartsWith("end-repeat")) {
                    if (!(block is RepeatBlock)) {
                        throw new IncorrectCommandException(i);
                    }
                    exprRegex = new Regex("end-repeat");
                    string remainder = exprRegex.Replace(command, string.Empty, 1);
                    if (remainder != string.Empty) {
                        throw new IncorrectCommandException(i);
                    }
                    return i;

                } else {
                    throw new IncorrectCommandException(i);
                }
            }
            return i;
        }
    }
}