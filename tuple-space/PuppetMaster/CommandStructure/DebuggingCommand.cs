namespace PuppetMaster.CommandStructure {
    public abstract class DebuggingCommand : Command {
        public DebuggingCommand(string processName) {
            this.ProcessName = processName;
        }

        public string ProcessName { get; }
    }
}