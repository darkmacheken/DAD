using System;
using System.Linq;
using Client.ScriptStructure;
using MessageService;
using MessageService.Serializable;

namespace Client {
    public class Client {
        private const string SERVERS_LIST = "..\\..\\..\\servers.txt";

        public string Id { get; }
        public Uri Url { get; }

        public int ViewNumber { get; set; }
        public Uri[] ViewServers { get; set; }
        public Uri Leader { get; set; }

        public int RequestNumber { get; set; }

        public MessageServiceClient MessageServiceClient { get; set; }

        public Script Script { get; }
        private readonly Parser parser;

        public Client(string id, Uri url, string scriptFile) {
            this.Id = id;
            this.Url = url;
            this.Script = new Script();
            this.parser = new Parser();
            this.SetScript(scriptFile);
            this.RequestNumber = 0;
            this.ViewNumber = 0;
            this.MessageServiceClient = new MessageServiceClient(this.Url);
        }

        public int GetRequestNumber() {
            return this.RequestNumber++;
        }

        private void SetScript(string scriptFile) {
            string[] lines = System.IO.File.ReadAllLines(@scriptFile); // relative to the executable's folder
            this.Script.Parse(this.parser, lines, 0);
        }

        public ClientHandShakeResponse DoHandShake() {
            // Do the handshake
            Uri[] servers = System.IO.File.ReadAllLines(SERVERS_LIST).ToList()
                .ConvertAll<Uri>(server => new Uri(server))
                .ToArray();

            IResponses responses = this.MessageServiceClient.RequestMulticast(
                new ClientHandShakeRequest(this.Id),
                servers,
                1,
                -1,
                true);
            ClientHandShakeResponse response = (ClientHandShakeResponse)responses.ToArray()[0];
            this.ViewNumber = response.ViewNumber;
            this.ViewServers = response.ViewConfiguration;
            this.Leader = response.Leader;

            return response;
        }
    }
}