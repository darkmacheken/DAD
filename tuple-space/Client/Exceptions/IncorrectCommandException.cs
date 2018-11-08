using System;
namespace Client.Exceptions {
    public class IncorrectCommandException : Exception {
        public IncorrectCommandException(int index) : base("Incorrect command in line: " + (index+1)) {
        }
    }
}
