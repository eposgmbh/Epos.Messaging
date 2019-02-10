using System;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Subscribes to integration commands and registers command handlers for them. </summary>
    public interface IIntegrationCommandSubscriber : IDisposable
    {
        /// <summary> Subscribes an integration command and registers its command handler. </summary>
        /// <returns>Task</returns>
        /// <param name="topic">Topic (optional) to further differentiate the integration command</param>
        Task Subscribe<C, CH>(string topic = null) where C : IntegrationCommand where CH : IntegrationCommandHandler<C>;
    }
}
