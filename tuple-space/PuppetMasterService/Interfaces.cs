using System;

namespace PuppetMasterService {

    public static class Constants {
        public const int PROCESS_CREATION_SERVICE_PORT = 10000;
        public const int PUPPET_MASTER_PORT = 10001;
        public const string PROCESS_CREATION_SERVICE = "pcs";
        public const string PUPPET_MASTER_SERVICE = "pms";
    }

    public interface IProcessCreationService {
        void CreateServer(string serverId, Uri url, int minDelay, int maxDelay, string protocol);

        void CreateClient(string clientId, Uri url, string scriptName);
    }

    public interface IPuppetMasterService {
        string Status();

        void Crash();

        void Freeze();

        void Unfreeze();
    }
}
