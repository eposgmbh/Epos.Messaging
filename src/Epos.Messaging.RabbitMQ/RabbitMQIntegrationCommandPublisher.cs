using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Epos.Messaging.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationCommandPublisher : IIntegrationCommandPublisher
    {
        private readonly IConnection myConnection;
        private readonly ConcurrentDictionary<int, IChannel> myChannels;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationCommandPublisher</b> class. </summary>
        /// <param name="options">Eventing options</param>
        public RabbitMQIntegrationCommandPublisher(IOptions<RabbitMQOptions> options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            myConnection = PersistentConnection.Create(options.Value);
            myChannels = new ConcurrentDictionary<int, IChannel>();
        }

        /// <inheritdoc />
        public async Task PublishAsync<C>(C c) where C : IntegrationCommand {
            if (c == null) {
                throw new ArgumentNullException(nameof(c));
            }

            string theRoutingKey = $"q-{c.GetType().Name}";

            if (!string.IsNullOrEmpty(c.Topic)) {
                theRoutingKey += $"-{c.Topic}";
            }

            IChannel theChannel = await GetChannelAsync().ConfigureAwait(false);

            await theChannel.QueueDeclareAsync(
                queue: theRoutingKey,
                durable: true,
                exclusive: false,
                autoDelete: false
            ).ConfigureAwait(false);

            string theMessage = JsonSerializer.Serialize(c);
            byte[] theBody = Encoding.UTF8.GetBytes(theMessage);

            var theProperties = new BasicProperties {
                Persistent = true
            };

            await theChannel.BasicPublishAsync(
                exchange: Constants.DefaultExchangeName,
                routingKey: theRoutingKey,
                mandatory: true,
                basicProperties: theProperties,
                body: theBody
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            if (myConnection.IsOpen) {
                await myConnection.CloseAsync().ConfigureAwait(false);
            }
        }

        #region --- Hilfsmethoden ---

        private async Task<IChannel> GetChannelAsync() {
            int theThreadId = Environment.CurrentManagedThreadId;

            if (!myChannels.TryGetValue(theThreadId, out IChannel? theResult)) {
                theResult = await myConnection.CreateChannelAsync().ConfigureAwait(false);
                myChannels[theThreadId] = theResult;
            }

            return theResult;
        }

        #endregion
    }
}
