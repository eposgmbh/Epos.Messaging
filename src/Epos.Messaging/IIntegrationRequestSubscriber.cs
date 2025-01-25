using System;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Subscribes to integration requests and registers request handlers for them. </summary>
    public interface IIntegrationRequestSubscriber : IAsyncDisposable
    {
        /// <summary> Subscribes a request and registers its request handler. </summary>
        /// <returns>Subscription</returns>
        /// <typeparam name="TRequest">Request class</typeparam>
        /// <typeparam name="TReply">Reply class</typeparam>
        /// <param name="topic">Topic (optional) to further differentiate the integration request</param>
        Task<ISubscription> SubscribeAsync<TRequest, TReply>(string? topic = null)
            where TRequest : IntegrationRequest where TReply : IntegrationReply, new();
    }
}
