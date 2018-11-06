namespace Client.ScriptStructure
{
    public class Wait : Command
    {
        private int time;

        public Wait(int time) : base() {
            this.time = time;
        }
    }
}