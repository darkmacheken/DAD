using System;

namespace MessageService {
    /// <summary>
    /// Remoting <c>interface</c> 
    /// </summary>
    public interface IMessageServiceServer {
        /// <summary>
        /// Processes the request.
        /// </summary>
        IResponse Request(ISenderInformation info, IMessage message);
    }

    /// <summary>
    /// Service wrapper, makes calls to <c>IMessageServiceServer</c>
    /// </summary>
    public interface IMessageServiceClient {
        /// <summary>
        /// Makes a simple request call
        /// </summary>
        IResponse Request(ISenderInformation info, IMessage message, Uri url);

        /// <summary>
        /// Makes a request call, returns <see langword="null"/> when timeouts..
        /// </summary>
        IResponse Request(ISenderInformation info, IMessage message, Uri url, int timeout);

        /// <summary>
        /// Multicasts a request. Waits for <paramref name="numberResponsesToWait"/> and returns.
        /// If <paramref name="numberResponsesToWait"/> is less than zero, it waits for all.
        /// If <paramref name="timeout"/> is less than zero it waits indefinitely.
        /// </summary>
        IResponses RequestMulticast(ISenderInformation info, IMessage message, Uri[] urls, int numberResponsesToWait, int timeout);
    }

    public interface IMessage {
        string ToString();
    }

    public interface ISenderInformation {
        string ToString();
    }

    public interface IResponse {
        string ToString();
    }

    public interface IResponses {
        void Add(IResponse response);

        IResponse[] ToArray();

        int Count();
    }


    public interface Protocol {
        IResponse ProcessRequest(ISenderInformation info, IMessage message);
    }
}
