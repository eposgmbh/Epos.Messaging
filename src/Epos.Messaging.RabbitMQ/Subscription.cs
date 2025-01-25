using System.Threading.Tasks;

using RabbitMQ.Client.Events;

namespace Epos.Messaging.RabbitMQ
{
    /// <inheritdoc />
    public class Subscription : ISubscription
    {
        /// <inheritdoc />
        public async ValueTask CancelAsync() {
            if (CancellingAsync is not null) {
                await CancellingAsync(this, AsyncEventArgs.Empty).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() => await CancelAsync();

        internal event AsyncEventHandler<AsyncEventArgs>? CancellingAsync;
    }
}
