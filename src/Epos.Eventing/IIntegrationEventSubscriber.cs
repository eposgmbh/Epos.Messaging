using System;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Subscribes to integration events and registers event handlers for them. </summary>
    public interface IIntegrationEventSubscriber : IDisposable
    {
        /// <summary> Subscribes an integration event and registers its event handler. </summary>
        /// <returns>Task</returns>
        Task Subscribe<E, EH>() where E : IntegrationEvent where EH : IntegrationEventHandler<E>;
    }
}
