using System.Collections.Generic;

namespace Client.ScriptStructure
{
    public abstract class Command : BasicNode
    {
        private string tuple = null;

        public Command() : base() {}

        public Command(string tuple) 
        {
            this.tuple = tuple;
        }
    }
}