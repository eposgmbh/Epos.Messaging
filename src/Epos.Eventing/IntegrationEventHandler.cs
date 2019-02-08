using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Base class for integration event handlers. </summary>
    public abstract class IntegrationEventHandler<E> where E : IntegrationEvent
    {
        /// <summary>
        ///  Handles an integration event. </summary>
        /// <param name="c">Integration event</param>
        /// <returns>Task</returns>
        public abstract Task Handle(E c);
    }
}
