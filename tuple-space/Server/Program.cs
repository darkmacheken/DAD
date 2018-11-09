using System;

using log4net.Config;

using MessageService;

using StateMachineReplication;

namespace Server
{
    public static class Program {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {
            string serverId = args[0];
            Uri url = new Uri(args[1]);
            string protocol = args[4];

            IProtocol protocolToUse = null;
            if (protocol.Equals("SMR")) {
                Log.Info("Using State Machine Replication protocol.");
                protocolToUse = new SMRProtocol();
            } else if(protocol.Equals("XL")) {
                Log.Info("Using Xu-Liskov protocol.");
                // TODO: assign XL protocol
            } else {
                Log.Fatal("Unknown protocol.");
                System.Environment.Exit(1);
            }

            // create message service wrapper
            ServerMessageWrapper serverMessage = new ServerMessageWrapper(
                url,
                protocolToUse,
                int.Parse(args[2]), 
                int.Parse(args[3]));

            // init ProtocolUsed
            protocolToUse.Init(serverMessage.ServiceClient, url, serverId);
            Console.ReadLine();
        }
    }
}
