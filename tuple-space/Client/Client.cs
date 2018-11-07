using System;
using Client.Exceptions;
using Client.ScriptStructure;
using Client.Visitor;

namespace Client {
    public class Client {
        private readonly string id;
        private readonly string url;
        private readonly Script script;
        private readonly Parser parser;

        public Client(string id, string url, string scriptFile) {
            this.id = id;
            this.url = url;
            this.script = new Script();
            this.parser = new Parser();
            this.SetScript(scriptFile);
        }

        private void SetScript(string scriptFile) {
            string[] lines = System.IO.File.ReadAllLines(@scriptFile); //relative to the executable's folder
            this.script.Parse(this.parser, lines, 0);
            Writer writer = new Writer();
            this.script.Accept(writer); //todo this is for testing, will delete further on
        }

        static void Main(string[] args) {
            try {
                Client client = new Client(args[0], args[1], args[2]);
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