using System;
using PuppetMaster.Exceptions;
using PuppetMaster.Visitor;

namespace PuppetMaster {
    class MainClass {
        public static void Main(string[] args) {
            try {
                //TODO check arguments
                PuppetMaster puppetMaster = new PuppetMaster(args[0]);

                puppetMaster.Script.Accept(new Writer());
            } catch (Exception ex) {
                if (ex is IncorrectCommandException) {
                    Console.WriteLine(ex.Message);
                } else {
                    throw;
                }
            }
        }
    }
}
