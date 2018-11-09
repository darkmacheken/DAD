using PuppetMaster.Visitor;

namespace PuppetMaster.CommandStructure {
    public abstract class DebuggingCommand : Command {
        private readonly string processName;

        public DebuggingCommand(string processName) {
            this.processName = processName;
        }

        public string ProcessName => this.processName;
    }
}