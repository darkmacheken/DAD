using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using PuppetMaster.CommandStructure;
using PuppetMaster.Exceptions;
using PuppetMaster.Visitor;

namespace PuppetMaster {
    public static class PuppetMaster {
        private static readonly int PUPPET_MASTER_PORT = 10001;

        public static void Main(string[] args) {
            // Create and register channel
            TcpChannel channel = new TcpChannel(PUPPET_MASTER_PORT);
            ChannelServices.RegisterChannel(channel, false);

            IBasicVisitor interpreter = new Interpreter();
            while (true) {
                try {
                    if (args.Length == 1) {
                        Console.WriteLine("Reading file...");
                        Script script = Parser.Parse(System.IO.File.ReadAllLines(args[0]));
                        script.Accept(interpreter);
                        break;
                    }

                    Console.Write(">>> ");
                    Parser.Parse(Console.ReadLine()).Accept(interpreter);

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
}
