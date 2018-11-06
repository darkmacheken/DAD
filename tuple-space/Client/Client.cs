using System;
using Client.ScriptStructure;

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
            string[] lines = System.IO.File.ReadAllLines(path: @scriptFile);
            this.script = new Script();
            script.Parse(parser, lines, 0);
        }

        static void Main(string[] args)
        {
        }
    }
}