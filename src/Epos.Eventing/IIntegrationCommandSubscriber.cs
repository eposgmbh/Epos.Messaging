using System;
using System.Threading;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Subscribes to integration commands and registers command handlers for them. </summary>
    public interface IIntegrationCommandSubscriber : IDisposable
    {
        /// <summary> Subscribes an integration command and registers its command handler. </summary>
        /// <returns>Task</returns>
        /// <typeparam name="C">Integration command class</typeparam>
        /// <param name="token">Cancellation token</param>
        /// <param name="topic">Topic (optional) to further differentiate the integration command</param>
        Task SubscribeAsync<C>(CancellationToken token, string topic = null) where C : IntegrationCommand;
    }
}
