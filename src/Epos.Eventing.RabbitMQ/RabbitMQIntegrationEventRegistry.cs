using System;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationEventRegistry : IIntegrationEventRegistry
    {
        private readonly IServiceProvider myServiceProvider;
        private readonly PersistentConnection myConnection;
        private IModel myChannel;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationEventRegistry</b> class. </summary>
        /// <param name="serviceProvider">Service provider to create <b>IntegrationEventHandler</b> instances</param>
        /// <param name="connectionFactory">Connection factory</param>
        public RabbitMQIntegrationEventRegistry(
            IServiceProvider serviceProvider, IConnectionFactory connectionFactory
        ) {
            this.myServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            if (connectionFactory == null) {
                throw new ArgumentNullException(nameof(connectionFactory));
            }
            myConnection = new PersistentConnection(connectionFactory);
        }

        /// <inheritdoc />
        public Task Register<E, EH>() where E : IntegrationEvent where EH : IntegrationEventHandler<E> {
            myConnection.EnsureIsConnected();

            if (myChannel == null) {
                myChannel = myConnection.CreateChannel();
            }

            var theRoutingKey = typeof(E).Name;
            string theQueueName = $"q-{theRoutingKey}";

            var theConsumer = new EventingBasicConsumer(myChannel);
            theConsumer.Received += async (model, ea) => {
                var theMessage = Encoding.UTF8.GetString(ea.Body);
                E theEvent = JsonConvert.DeserializeObject<E>(theMessage);
                EH theHandler = (EH) myServiceProvider.GetService(typeof(EH));

                await theHandler.Handle(
                    theEvent,
                    new MessagingHelper(
                        ack: () => myChannel.BasicAck(ea.DeliveryTag, multiple: false)
                    )
                );
            };

            myChannel.BasicConsume(queue: theQueueName,
                                   autoAck: false,
                                   consumer: theConsumer);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() {
            myConnection.Dispose();
        }
    }
}
