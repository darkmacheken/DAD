using System;
using Client.ScriptStructure;
using Client.Visitor;

namespace Client
{
    public class Client
    {
        private string id;
        private string url;
        private Script script;
        private Parser parser;

        public Client(string id, string url, string scriptFile) 
        {
            this.id = id;
            this.url = url;
            this.parser = new Parser();
            this.setScript(scriptFile);
        }

        private void setScript(string scriptFile)
        {
            //string[] documents = System.IO.Directory.GetFiles(scriptFile);
            //string[] lines = System.IO.File.ReadAllLines(path: @scriptFile);
            string[] lines = { "add <\"a\",DADTestA(1,\"b\")>", "begin-repeat 3", "read <\"*\",null>", "wait 500", "read <\"a*\",DADTestA>", "read <\"*a\",DADTestA(1,\"b\")>", "end-repeat", "add <\"xx\",DADTestB(1,\"c\",2)>" };
            this.script = new Script();
            script.Parse(parser, lines, 0);
            Writer writer = new Writer();
            script.Accept(writer);
        }

        static void Main(string[] args)
        {
            Client c1 = new Client(args[0], args[1], args[2]);
        }
    }
}