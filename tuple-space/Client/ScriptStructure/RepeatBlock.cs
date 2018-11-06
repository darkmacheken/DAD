namespace Client.ScriptStructure
{
    public class RepeatBlock : Block
    {
        private int numRepeats;

        public RepeatBlock(int numRepeats) : base() {
            this.numRepeats = numRepeats;
        }
    }
}