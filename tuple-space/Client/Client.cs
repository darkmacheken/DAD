using System;
using Client.Exceptions;
using Client.ScriptStructure;
using Client.Visitor;

namespace Client {
    public class Client {
        private string id;
        private string url;
        private Script script;
        private Parser parser;

        public Client(string id, string url, string scriptFile) {
            this.id = id;
            this.url = url;
            this.parser = new Parser();
            this.setScript(scriptFile);
        }

        private void setScript(string scriptFile) {
            string[] lines = System.IO.File.ReadAllLines(path: @scriptFile); //relative to the executable's folder
            this.script = new Script();
            script.Parse(parser, lines, 0);
            Writer writer = new Writer();
            script.Accept(writer);
        }

        static void Main(string[] args) {
            try {
                Client c1 = new Client(args[0], args[1], args[2]);
            } catch (Exception ex) {
                if (ex is IncorrectCommandException || ex is BlockEndMissingException) {
                    Console.WriteLine(ex.Message);
                } else {
                    throw;
                }
            }
        }
    }
}