using System;

using Client.Exceptions;
using Client.Visitor;

using MessageService;

namespace Client {
    public static class Program {
        static void Main(string[] args) {
            try {
                //TODO check arguments
                Client client = new Client(args[0], new Uri(args[1]), args[2]);

                MessageServiceClient messageServiceClient = new MessageServiceClient(client.Url);

                client.Script.Accept(new Executor(messageServiceClient, client));

                Console.ReadLine();
            } catch (Exception ex) {
                if (ex is IncorrectCommandException || ex is BlockEndMissingException) {
                    Console.WriteLine(ex.Message);
                } else {
                    throw;
                }
            }
        }
    }
}