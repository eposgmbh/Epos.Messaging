using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Base class for integration command handlers. </summary>
    public abstract class IntegrationCommandHandler<C> where C : IntegrationCommand
    {
        /// <summary>
        ///  Handles an integration command. </summary>
        /// <param name="c">Integration command</param>
        /// <param name="h">Messaging helper functionalities</param>
        /// <returns>Task</returns>
        public abstract Task Handle(C c, MessagingHelper h);
    }
}
