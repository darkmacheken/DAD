namespace Client.ScriptStructure {
    public abstract class Command : BasicNode {
        private readonly string tuple;

        public Command(string tuple) {
            this.tuple = tuple;
        }

        public string Tuple => this.tuple;
    }
}