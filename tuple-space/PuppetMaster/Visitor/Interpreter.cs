using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PuppetMaster.CommandStructure;
using PuppetMasterService;

namespace PuppetMaster.Visitor {
    public class Interpreter : IBasicVisitor {
        private readonly ConcurrentDictionary<string, Uri> servers;
        private readonly ConcurrentDictionary<string, Uri> clients;

        public Interpreter() {
            this.servers = new ConcurrentDictionary<string, Uri>();
            this.clients = new ConcurrentDictionary<string, Uri>();
        }

        public void VisitCrash(Crash crash) {
            Console.WriteLine($"Crash {crash.ProcessName}");
        }

        public void VisitCreateClient(CreateClient createClient) {
            Task.Factory.StartNew(() => {
                if (this.clients.ContainsKey(createClient.Id)) {
                    return;
                }

                this.clients.TryAdd(createClient.Id, createClient.Url);
                IProcessCreationService processCreationService = GetProcessCreationService(createClient.Url);

                processCreationService.CreateClient(
                    createClient.Id,
                    createClient.Url,
                    createClient.ScriptFile);

            });
        }

        public void VisitCreateServer(CreateServer createServer) {
            Task.Factory.StartNew(() => {
                if (this.servers.ContainsKey(createServer.Id)) {
                    return;
                }

                this.servers.TryAdd(createServer.Id, createServer.Url);

                IProcessCreationService processCreationService = GetProcessCreationService(createServer.Url);
                processCreationService.CreateServer(
                    createServer.Id, 
                    createServer.Url, 
                    createServer.MinDelay, 
                    createServer.MaxDelay, 
                    createServer.Protocol);
            });
        }

        public void VisitFreeze(Freeze freeze) {
            Console.WriteLine($"Freeze {freeze.ProcessName}");
        }

        public void VisitScript(Script script) {
            foreach (Command node in script.Nodes) {
                node.Accept(this);
            }
        }

        public void VisitStatus(Status status) {
            StringBuilder statusMessage = new StringBuilder();
            foreach (KeyValuePair<string, Uri> entry in this.servers) {
                statusMessage.Append(
                    $"=============================================================================={Environment.NewLine}" +
                    $"================================= SERVER INFO ================================{Environment.NewLine}" +
                    $"=============================================================================={Environment.NewLine}" +
                    $"Server ID: {entry.Key} {Environment.NewLine}");

                try {
                    IPuppetMasterService server = GetServerService(entry.Value);

                    statusMessage.Append(server.Status());
                } catch (System.Net.Sockets.SocketException) {
                    statusMessage.Append($"Server is dead. {Environment.NewLine}");
                }

                statusMessage.Append(
                    $"=============================================================================={Environment.NewLine}");
            }
            Console.Write(statusMessage.ToString());
        }

        public void VisitUnfreeze(Unfreeze unfreeze) {
            Console.WriteLine($"Unfreeze {unfreeze.ProcessName}");
        }

        public void VisitWait(Wait wait) {
            Thread.Sleep(wait.Time);
        }

        public void VisitExit(Exit exit) {
            Environment.Exit(0);
        }

        private IProcessCreationService GetProcessCreationService(Uri url) {
            return (IProcessCreationService)
                Activator.GetObject(
                    typeof(IProcessCreationService),
                    $"tcp://{url.Host}:{Constants.PROCESS_CREATION_SERVICE_PORT}/{Constants.PROCESS_CREATION_SERVICE}");
        }

        private IPuppetMasterService GetServerService(Uri url) {
            return (IPuppetMasterService)
                Activator.GetObject(
                    typeof(IPuppetMasterService),
                    $"tcp://{url.Host}:{url.Port}/{Constants.PUPPET_MASTER_SERVICE}");
        }
    }
}
