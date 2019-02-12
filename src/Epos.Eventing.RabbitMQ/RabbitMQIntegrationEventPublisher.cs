using System;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RabbitMQ.Client;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly PersistentConnection myConnection;
        private IModel myChannel;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationEventPublisher</b> class. </summary>
        /// <param name="connectionFactory">Connection factory</param>
        public RabbitMQIntegrationEventPublisher(IConnectionFactory connectionFactory) {
            if (connectionFactory == null) {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            myConnection = new PersistentConnection(connectionFactory);
        }

        /// <inheritdoc />
        public Task Publish<E>(E e) where E : IntegrationEvent {
            if (e == null) {
                throw new ArgumentNullException(nameof(e));
            }

            myConnection.EnsureIsConnected();

            if (myChannel == null) {
                myChannel = myConnection.CreateChannel();
            }

            string theExchangeName = $"e-{e.GetType().Name}";

            myChannel.ExchangeDeclare(exchange: theExchangeName, type: "fanout", durable: true);

            string theMessage = JsonConvert.SerializeObject(e);
            byte[] theBody = Encoding.UTF8.GetBytes(theMessage);

            myChannel.BasicPublish(
                exchange: theExchangeName,
                routingKey: string.Empty,
                basicProperties: null,
                body: theBody
            );

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() {
            myChannel.Dispose();
            myConnection.Dispose();
        }
    }
}
