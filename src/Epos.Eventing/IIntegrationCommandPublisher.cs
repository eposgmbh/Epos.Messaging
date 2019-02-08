using System;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Publishes integration commands. </summary>
    public interface IIntegrationCommandPublisher : IDisposable
    {
        /// <summary> Publishes an integration command. </summary>
        /// <param name="c">Integration command</param>
        /// <returns>Task</returns>
        Task Publish<C>(C c) where C : IntegrationCommand;
    }
}
