using System;

namespace Epos.Eventing
{
    /// <summary> Base class for integration events. </summary>
    public abstract class IntegrationEvent
    {
        /// <summary> Creates an instance of the <b>IntegrationEvent</b> class. </summary>
        protected IntegrationEvent() {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        /// <summary> Message Id. </summary>
        /// <returns>Id</returns>
        public Guid Id { get; }

        /// <summary> Message creation date. </summary>
        /// <returns>Creation date</returns>
        public DateTime CreationDate { get; }
    }
}
