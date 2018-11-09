using System;

using Client.ScriptStructure;

namespace Client {
    public class Client {
        public string Id { get; }
        public Uri Url { get; }
        public int ViewId { get; }

        public int RequestNumber { get; set; }

        public Script Script { get; }
        private readonly Parser parser;

        public Client(string id, Uri url, string scriptFile) {
            this.Id = id;
            this.Url = url;
            this.Script = new Script();
            this.parser = new Parser();
            this.SetScript(scriptFile);
            this.RequestNumber = 0;
            this.ViewId = 0;
        }

        public int GetRequestNumber() {
            return this.RequestNumber++;
        }

        private void SetScript(string scriptFile) {
            string[] lines = System.IO.File.ReadAllLines(@scriptFile); // relative to the executable's folder
            this.Script.Parse(this.parser, lines, 0);
        }
        
    }
}