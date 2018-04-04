using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Base class for integration event handlers. </summary>
    public abstract class IntegrationEventHandler<E> where E : IntegrationEvent
    {
        /// <summary>
        ///  Handles an integration event. </summary>
        /// <param name="e">Integration event</param>
        /// <param name="h">Messaging helper functionalities</param>
        /// <returns>Task</returns>
        public abstract Task Handle(E e, MessagingHelper h);
    }
}
