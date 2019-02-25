using System;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Subscribes to integration commands and registers command handlers for them. </summary>
    public interface IIntegrationCommandSubscriber : IDisposable
    {
        /// <summary> Subscribes an integration command and registers its command handler. </summary>
        /// <returns>Subscription</returns>
        /// <typeparam name="C">Integration command class</typeparam>
        /// <param name="topic">Topic (optional) to further differentiate the integration command</param>
        Task<ISubscription> SubscribeAsync<C>(string? topic = null) where C : IntegrationCommand;
    }
}
