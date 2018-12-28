using System;
using System.Runtime.Remoting;
using MessageService;
using PuppetMasterService;

namespace Server {
    public class PuppetMasterServer : MarshalByRefObject, IPuppetMasterService {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(PuppetMasterServer));

        private readonly IProtocol protocol;
        private readonly ServerMessageWrapper serverMessageLayer;

        public PuppetMasterServer(IProtocol protocol, ServerMessageWrapper serverMessageLayer) {
            this.protocol = protocol;
            this.serverMessageLayer = serverMessageLayer;

            // Register Service
            RemotingServices.Marshal(
                this,
                PuppetMasterService.Constants.PUPPET_MASTER_SERVICE,
                typeof(PuppetMasterServer));
        }
        
        public void Crash() {
            Log.Fatal("Crash command issued.");
            Environment.Exit(1);
        }

        public void Freeze() {
            Log.Info("Freeze command issued.");
            this.serverMessageLayer.Freeze();
        }

        public void Unfreeze() {
            Log.Info("Unfreeze command issued.");
            this.serverMessageLayer.Unfreeze();
        }

        public string Status() {
            Log.Info("Status command issued.");
            string status =
                $"-------------------------------- MESSAGE LAYER -------------------------------{Environment.NewLine}" +
                $"{this.serverMessageLayer.Status()}" +
                $"------------------------------- PROTOCOL LAYER -------------------------------{Environment.NewLine}" +
                $"{this.protocol.Status()}" +
                $"=============================================================================={Environment.NewLine}";
            return status;
        }

        public override object InitializeLifetimeService() {
            return null;
        }
    }
}