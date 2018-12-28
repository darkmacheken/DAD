using System;
namespace PuppetMaster.Exceptions {
    public class IncorrectCommandException : Exception {
        public IncorrectCommandException(int index, string message) : base($"Incorrect command in line {index+1}: {message}") {
        }

        public IncorrectCommandException(int index) : base($"Incorrect command in line {index + 1}") {
        }
    }
}
