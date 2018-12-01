using System;
using System.Threading;
using System.Threading.Tasks;
using PuppetMaster.CommandStructure;
using PuppetMasterService;

namespace PuppetMaster.Visitor {
    public class Interpreter : IBasicVisitor {
        public Interpreter() {}

        public void VisitCrash(Crash crash) {
            Console.WriteLine($"Crash {crash.ProcessName}");
        }

        public void VisitCreateClient(CreateClient createClient) {
            Task.Factory.StartNew(() => {
                IProcessCreationService processCreationService = GetProcessCreationService(createClient.Url);

                processCreationService.CreateClient(
                    createClient.Id, 
                    createClient.Url, 
                    createClient.ScriptFile);
            });
        }

        public void VisitCreateServer(CreateServer createServer) {
            Task.Factory.StartNew(() => {
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
            Console.WriteLine("Status");
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
    }
}
