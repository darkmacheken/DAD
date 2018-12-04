using System;
using System.Linq;
using Client.Exceptions;
using Client.Visitor;

using MessageService;
using MessageService.Serializable;

namespace Client {
    public static class Program {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));

        private const string SERVERS_LIST = "..\\..\\..\\servers.txt";

        static void Main(string[] args) {
            try {
                //TODO check arguments
                Client client = new Client(args[0], new Uri(args[1]), args[2]);

                MessageServiceClient messageServiceClient = new MessageServiceClient(client.Url);

                // Do the handshake
                Uri[] servers = System.IO.File.ReadAllLines(SERVERS_LIST).ToList()
                    .ConvertAll<Uri>(server => new Uri(server))
                    .ToArray();

                IResponses responses = messageServiceClient.RequestMulticast(
                    new ClientHandShakeRequest(client.Id),
                    servers,
                    1,
                    -1,
                    true);
                ClientHandShakeResponse response = (ClientHandShakeResponse)responses.ToArray()[0];
                client.ViewNumber = response.ViewNumber;
                client.ViewServers = response.ViewConfiguration;
                client.Leader = response.Leader;
                
                switch (response.ProtocolUsed) {
                    case Protocol.StateMachineReplication:
                        Log.Info("Handshake: Using State Machine Replication protocol.");
                        client.Script.Accept(new SMRExecuter(messageServiceClient, client));
                        break;
                    case Protocol.XuLiskov:
                        Log.Info("Handshake: Using Xu-Liskov protocol");
                        client.Script.Accept(v: new XLExecuter(messageServiceClient, client));
                        break;
                    default:
                        Log.Fatal("Unknown protocol.");
                        Environment.Exit(1);
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