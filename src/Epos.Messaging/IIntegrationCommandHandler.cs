using System.Threading;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Interface for integration command handlers. </summary>
    public interface IIntegrationCommandHandler<C> where C : IntegrationCommand
    {
        /// <summary>
        ///  Handles an integration command. </summary>
        /// <param name="c">Integration command</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="h">Command helper functionalities</param>
        /// <returns>Task</returns>
        Task Handle(C c, CancellationToken token, IIntegrationCommandHelper h);
    }
}
