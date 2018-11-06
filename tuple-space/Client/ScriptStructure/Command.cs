using System.Collections.Generic;

namespace Client.ScriptStructure
{
    public abstract class Command : BasicNode
    {
        private string tuple;

        public Command(string tuple) 
        {
            this.tuple = tuple;
        }

        public string Tuple
        {
            get
            {
                return tuple;
            }
        }
    }
}