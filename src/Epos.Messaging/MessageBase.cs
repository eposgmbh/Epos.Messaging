using System;

namespace Epos.Messaging
{
    /// <summary> Base class for messages. </summary>
    public abstract class MessageBase
    {
        /// <summary> Message Id. </summary>
        /// <returns>Id</returns>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary> Message creation date. </summary>
        /// <returns>Creation date</returns>
        public DateTime CreationDate { get; } = DateTime.UtcNow;
    }
}
