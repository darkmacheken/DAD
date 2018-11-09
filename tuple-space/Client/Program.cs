using System;

using Client.Exceptions;
using Client.Visitor;

using MessageService;
using MessageService.Serializable;

namespace Client {
    public static class Program {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {
            try {
                //TODO check arguments
                Client client = new Client(args[0], new Uri(args[1]), args[2]);

                MessageServiceClient messageServiceClient = new MessageServiceClient(client.Url);

                // Do the handshake
                HandShakeResponse response = (HandShakeResponse)messageServiceClient.Request(
                    new HandShakeRequest(client.Id),
                    new Uri("tcp://localhost:8080"));

                
                switch (response.ProtocolUsed) {
                    case Protocol.StateMachineReplication:
                        Log.Info("Handshake: Using State Machine Replication protocol.");
                        client.Script.Accept(new SMRExecuter(messageServiceClient, client));
                        break;
                    case Protocol.XuLiskov:
                        Log.Info("Handshake: Using Xu-Liskov protocol");
                        // TODO: call the visitor
                        break;
                    default:
                        Log.Fatal("Unknown protocol.");
                        System.Environment.Exit(1);
                        break;
                }

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