using System;
using PuppetMaster.CommandStructure;

namespace PuppetMaster.Visitor {
    public class Writer : IBasicVisitor {
        public Writer() {}

        public void VisitCrash(Crash crash) {
            Console.WriteLine($"Crash {crash.ProcessName}");
        }

        public void VisitCreateClient(CreateClient createClient) {
            Console.WriteLine($"Client {createClient.Id} {createClient.Url} {createClient.ScriptFile}");
        }

        public void VisitCreateServer(CreateServer createServer) {
            Console.WriteLine($"Server {createServer.Id} {createServer.Url} {createServer.MinDelay} {createServer.MaxDelay}");
        }

        public void VisitFreeze(Freeze freeze) {
            Console.WriteLine($"Freeze {freeze.ProcessName}");
        }

        public void VisitScript(Script script) {
            Console.WriteLine("Script:\n");
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
            Console.WriteLine($"Wait {wait.Time}");
        }

        public void VisitExit(Exit exit) {
            Console.WriteLine($"Exit");
        }
    }
}
