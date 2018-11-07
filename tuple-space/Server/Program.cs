using System;

using MessageService;

using StateMachineReplication;

namespace Server
{
    class Program {
        static void Main(string[] args) {
            string serverId = args[0];
            IProtocol protocol = new SMRProtocol();
            Uri url = new Uri(args[1]);

            // create message service wrapper
            MessageServiceWrapper messageService = new MessageServiceWrapper(
                url,
                protocol,
                int.Parse(args[2]), 
                int.Parse(args[3]));

            // init protocol
            protocol.Init(messageService.ServiceClient, url, serverId);
            Console.ReadLine();
        }
    }
}
