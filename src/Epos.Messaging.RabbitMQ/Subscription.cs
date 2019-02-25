using System;

namespace Epos.Messaging.RabbitMQ
{
    /// <inheritdoc />
    public class Subscription : ISubscription
    {
        /// <inheritdoc />
        public void Cancel() => Cancelling?.Invoke(this, EventArgs.Empty);

        /// <inheritdoc />
        public void Dispose() => Cancel();

        internal event EventHandler Cancelling;
    }
}
