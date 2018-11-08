using System;
using Client.Exceptions;
using Client.ScriptStructure;
using Client.Visitor;

using MessageService;

namespace Client {
    public class Client {
        public string Id { get; }
        public Uri Url { get; }

        private int requestNumber;

        public Script Script { get; }
        private readonly Parser parser;

        public Client(string id, Uri url, string scriptFile) {
            this.Id = id;
            this.Url = url;
            this.Script = new Script();
            this.parser = new Parser();
            this.SetScript(scriptFile);
            this.requestNumber = 0;
        }

        public int GetRequestNumber() {
            return this.requestNumber++;
        }

        private void SetScript(string scriptFile) {
            string[] lines = System.IO.File.ReadAllLines(@scriptFile); // relative to the executable's folder
            this.Script.Parse(this.parser, lines, 0);
        }
        
    }
}