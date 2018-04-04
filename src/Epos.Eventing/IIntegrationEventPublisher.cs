using System;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Publishes integration events. </summary>
    public interface IIntegrationEventPublisher : IDisposable
    {
        /// <summary> Publishes an integration event. </summary>
        /// <param name="e">Integration event</param>
        /// <returns>Task</returns>
        Task Publish<E>(E e) where E : IntegrationEvent;
    }
}
