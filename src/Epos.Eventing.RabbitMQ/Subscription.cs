using System;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class Subscription : ISubscription
    {
        /// <inheritdoc />
        public void Cancel() => Cancelling?.Invoke(this, EventArgs.Empty);

        internal event EventHandler Cancelling;
    }
}
