using System;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Publishes an integration request and waits for an asynchronous reply. </summary>
    public interface IIntegrationRequestPublisher : IDisposable
    {
        /// <summary> Publishes a request and waits for an asynchronous reply. </summary>
        /// <remarks> Throws a <see cref="TimeoutException"/>, if no reply is sent in the time set by
        /// the <paramref name="timeoutSeconds"/> parameter. </remarks>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TReply">Reply type</typeparam>
        /// <param name="request">Request</param>
        /// <param name="timeoutSeconds">Timeout in seconds (default: 5)</param>
        /// <returns>Reply</returns>
        Task<TReply> PublishAsync<TRequest, TReply>(TRequest request, int timeoutSeconds = 5)
            where TRequest : IntegrationRequest where TReply : IntegrationReply, new();
    }
}
