using System;

namespace Epos.Eventing
{
    /// <summary> Base class for integration events and commands. </summary>
    public abstract class IntegrationEventCommandBase
    {
        /// <summary> Message Id. </summary>
        /// <returns>Id</returns>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary> Message creation date. </summary>
        /// <returns>Creation date</returns>
        public DateTime CreationDate { get; } = DateTime.UtcNow;
    }
}
