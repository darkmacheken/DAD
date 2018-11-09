using System;

using MessageService;

using StateMachineReplication;

namespace Server
{
    public static class Program {
        static void Main(string[] args) {
            string serverId = args[0];
            IProtocol protocol = new SMRProtocol();
            Uri url = new Uri(args[1]);

            // create message service wrapper
            ServerMessageWrapper serverMessage = new ServerMessageWrapper(
                url,
                protocol,
                int.Parse(args[2]), 
                int.Parse(args[3]));

            // init protocol
            protocol.Init(serverMessage.ServiceClient, url, serverId);
            Console.ReadLine();
        }
    }
}
