using System;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Registers integration events and connects them to event handlers. </summary>
    public interface IIntegrationEventRegistry : IDisposable
    {
        /// <summary> Registers an integration event and its event handler. </summary>
        /// <returns>Task</returns>
        Task Register<E, EH>() where E : IntegrationEvent where EH : IntegrationEventHandler<E>;
    }
}
