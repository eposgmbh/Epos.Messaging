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
        private const string ExchangeName = nameof(RabbitMQIntegrationEventPublisher);

        private readonly PersistentConnection myConnection;

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

            using (var theChannel = myConnection.CreateChannel()) {
                var theRoutingKey = e.GetType().Name;
                string theQueueName = $"q-{theRoutingKey}";

                theChannel.ExchangeDeclare(exchange: ExchangeName, type: "topic", durable: true);
                theChannel.QueueDeclare(queue: theQueueName, durable: true, exclusive: false, autoDelete: false);
                theChannel.QueueBind(queue: theQueueName, exchange: ExchangeName, routingKey: theRoutingKey);

                var theMessage = JsonConvert.SerializeObject(e);
                var theBody = Encoding.UTF8.GetBytes(theMessage);

                IBasicProperties theBasicProperties = theChannel.CreateBasicProperties();
                theBasicProperties.Persistent = true;

                theChannel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: theRoutingKey,
                    basicProperties: theBasicProperties,
                    body: theBody
                );
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() {
            myConnection.Dispose();
        }
    }
}
