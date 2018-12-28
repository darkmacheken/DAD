using System;
using System.ComponentModel;
using System.Diagnostics;
using PuppetMasterService;

namespace ProcessCreationService {

    public class ProcessCreationService : MarshalByRefObject, IProcessCreationService {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ProcessCreationService));

        private const string BASE_DIR = "..\\..\\..\\";
        private const string SERVER_EXE = BASE_DIR + "Server\\bin\\Debug\\Server.exe";
        private const string CLIENT_EXE = BASE_DIR + "Client\\bin\\Debug\\Client.exe";

        public void CreateClient(string clientId, Uri url, string scriptName) {
            Log.Info($"Trying to initialize client: ClientId = {clientId}, url = {url}, scriptName = {scriptName}");
            try {
                Process process = new Process {
                    StartInfo = {
                        FileName = CLIENT_EXE,
                        Arguments = $"{clientId} {url} {scriptName}"
                    }
                };

                process.Start();
            } catch (Win32Exception e) {
                Log.Error($"{e.Message} Could not initialize client: ClientId = {clientId}, url = {url}, scriptName = {scriptName}");
            }
        }

        public void CreateServer(string serverId, Uri url, int minDelay, int maxDelay, string protocol) {
            Log.Info($"Trying to initialize server: ServerId = {serverId}, url = {url}, " +
                     $"minDelay = {minDelay}, maxDelay = {maxDelay}, protocol = {protocol}");
            try {
                Process process = new Process {
                    StartInfo = {
                        FileName = SERVER_EXE,
                        Arguments = $"{serverId} {url} {minDelay} {maxDelay} {protocol}"
                    }
                };

                process.Start();
            } catch (Win32Exception e) {
                Log.Error($"{e.Message} Could not initialize client: ServerId = {serverId}, url = {url}, " +
                          $"minDelay = {minDelay}, maxDelay = {maxDelay}, protocol = {protocol}");
            }
        }

        public override object InitializeLifetimeService() {
            return null;
        }
    }
}