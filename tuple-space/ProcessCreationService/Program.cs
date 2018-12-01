using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace ProcessCreationService {
    class Program {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {
            // Create and register channel
            TcpChannel channel = new TcpChannel(PuppetMasterService.Constants.PROCESS_CREATION_SERVICE_PORT);
            ChannelServices.RegisterChannel(channel, false);
            Log.Info($"TCP channel created at port {PuppetMasterService.Constants.PROCESS_CREATION_SERVICE_PORT}");
            
            // Register service
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(ProcessCreationService),
                PuppetMasterService.Constants.PROCESS_CREATION_SERVICE,
                WellKnownObjectMode.Singleton);

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}
