using System;
namespace Client.Exceptions {
    public class BlockEndMissingException : Exception {
        public BlockEndMissingException(int index) : base("end-repeat keyword is missing: " + (index+1)) {
        }
    }
}
