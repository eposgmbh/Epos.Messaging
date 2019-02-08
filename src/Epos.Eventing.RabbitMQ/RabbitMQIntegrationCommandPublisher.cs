using System;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RabbitMQ.Client;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationCommandPublisher : IIntegrationCommandPublisher
    {
        private static readonly string DefaultExchangeName = string.Empty;

        private readonly PersistentConnection myConnection;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationCommandPublisher</b> class. </summary>
        /// <param name="connectionFactory">Connection factory</param>
        public RabbitMQIntegrationCommandPublisher(IConnectionFactory connectionFactory) {
            if (connectionFactory == null) {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            myConnection = new PersistentConnection(connectionFactory);
        }

        /// <inheritdoc />
        public Task Publish<C>(C c) where C : IntegrationCommand {
            if (c == null) {
                throw new ArgumentNullException(nameof(c));
            }

            myConnection.EnsureIsConnected();

            using (IModel theChannel = myConnection.CreateChannel()) {
                string theRoutingKey = $"q-{c.GetType().Name}";

                theChannel.QueueDeclare(queue: theRoutingKey, durable: true, exclusive: false, autoDelete: false);

                string theMessage = JsonConvert.SerializeObject(c);
                byte[] theBody = Encoding.UTF8.GetBytes(theMessage);

                IBasicProperties theProperties = theChannel.CreateBasicProperties();
                theProperties.Persistent = true;

                theChannel.BasicPublish(
                    exchange: DefaultExchangeName,
                    routingKey: theRoutingKey,
                    basicProperties: theProperties,
                    body: theBody
                );
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() => myConnection.Dispose();
    }
}
