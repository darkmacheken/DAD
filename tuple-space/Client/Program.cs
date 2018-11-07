using System;

using MessageService;

namespace Client {
    class Program {
        static void Main(string[] args) {

            Uri[] urls = { new Uri("tcp://localhost:8080"), new Uri("tcp://localhost:8081"), new Uri("tcp://localhost:8082") };

            string clientId = args[0];
            Uri url = new Uri(args[1]);

            MessageServiceClient messageServiceClient = new MessageServiceClient(url);

            IResponses responses = messageServiceClient.RequestMulticast(new TestSenderInformation(clientId), new TestMessage("Pedro"), urls, 2, 5);

            if (responses == null) {
                Console.WriteLine("Timeout");
            } else {
                foreach (IResponse Iresponse in responses.ToArray()) {
                    TestResponse response = (TestResponse)Iresponse;

                    Console.WriteLine(response.Response);
                }
            }
            
            
            Console.ReadLine();
        }
    }
}
