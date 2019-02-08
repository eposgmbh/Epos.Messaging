using System;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Subscribes to integration commands and registers command handlers for them. </summary>
    public interface IIntegrationCommandSubscriber : IDisposable
    {
        /// <summary> Subscribes an integration command and registers its command handler. </summary>
        /// <returns>Task</returns>
        Task Subscribe<C, CH>() where C : IntegrationCommand where CH : IntegrationCommandHandler<C>;
    }
}
