using System;
using PuppetMaster.CommandStructure;

namespace PuppetMaster {
    public class PuppetMaster {
        public Script Script { get; set; }
        private readonly Parser parser;

        public PuppetMaster() {
            this.parser = new Parser();
        }

        public PuppetMaster(string scriptFile) {
            this.Script = new Script();
            this.parser = new Parser();
            this.SetScript(scriptFile);
        }

        private void SetScript(string scriptFile) {
            string[] lines = System.IO.File.ReadAllLines(@scriptFile); // relative to the executable's folder
            this.Script = this.parser.Parse(lines);
        }

    }
}
