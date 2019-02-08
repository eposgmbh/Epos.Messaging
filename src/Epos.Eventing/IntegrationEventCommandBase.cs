using System;

namespace Epos.Eventing
{
    /// <summary> Base class for integration events and commands. </summary>
    public abstract class IntegrationEventCommandBase
    {
        /// <summary> Creates an instance of the <b>IntegrationCommand</b> class. </summary>
        protected IntegrationEventCommandBase() {
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
