using System;
using Client.Exceptions;
using Client.ScriptStructure;
using Client.Visitor;

using MessageService;

namespace Client {
    public class Client {
        public string Id { get; }

        private readonly Uri Url;

        public Script Script { get; }

        private readonly Parser parser;

        public Client(string id, Uri url, string scriptFile) {
            this.Id = id;
            this.Url = url;
            this.Script = new Script();
            this.parser = new Parser();
            this.SetScript(scriptFile);
        }

        private void SetScript(string scriptFile) {
            string[] lines = System.IO.File.ReadAllLines(@scriptFile); // relative to the executable's folder
            this.Script.Parse(this.parser, lines, 0);
        }

        static void Main(string[] args) {
            try {
                //TODO check arguments
                Client client = new Client(args[0], new Uri(args[1]), args[2]);
                
                MessageServiceClient messageServiceClient = new MessageServiceClient(client.Url);

                client.Script.Accept(new Executor(messageServiceClient, client));

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