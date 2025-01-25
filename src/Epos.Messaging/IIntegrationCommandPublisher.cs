using System;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Publishes integration commands. </summary>
    public interface IIntegrationCommandPublisher : IAsyncDisposable
    {
        /// <summary> Publishes an integration command. </summary>
        /// <typeparam name="C">Integration command class</typeparam>
        /// <param name="c">Integration command</param>
        /// <returns>Task</returns>
        Task PublishAsync<C>(C c) where C : IntegrationCommand;
    }
}
