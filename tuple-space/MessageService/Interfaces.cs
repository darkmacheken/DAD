using System;

namespace MessageService {
    using System.Security.Policy;

    /// <summary>
    /// Remoting <c>interface</c> 
    /// </summary>
    public interface IMessageServiceBasic {
        void Request(ISenderInformation info, IMessage message);

        void Send(ISenderInformation info, IMessage message);

    }

    /// <summary>
    /// Service wrapper, makes calls to <c>IMessageServiceBasic</c>
    /// </summary>
    public interface IMessageService {
        void Request(ISenderInformation info, IMessage message, Uri url);

        void Send(ISenderInformation info, IMessage message, Uri url);

        void RequestMulticast(ISenderInformation info, IMessage message, Uri[] urls);

        void SendMulticast(ISenderInformation info, IMessage message, Uri[] urls);
    }

    public interface IMessage { }

    public interface ISenderInformation { }
}
