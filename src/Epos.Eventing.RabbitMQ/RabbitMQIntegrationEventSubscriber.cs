using System;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationEventSubscriber : IIntegrationEventSubscriber
    {
        private readonly IServiceProvider myServiceProvider;
        private readonly PersistentConnection myConnection;
        private IModel myChannel;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationEventSubscriber</b> class. </summary>
        /// <param name="serviceProvider">Service provider to create <b>IntegrationEventHandler</b> instances</param>
        /// <param name="connectionFactory">Connection factory</param>
        public RabbitMQIntegrationEventSubscriber(
            IServiceProvider serviceProvider, IConnectionFactory connectionFactory
        ) {
            this.myServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            if (connectionFactory == null) {
                throw new ArgumentNullException(nameof(connectionFactory));
            }
            myConnection = new PersistentConnection(connectionFactory);
        }

        /// <inheritdoc />
        public Task Subscribe<E, EH>() where E : IntegrationEvent where EH : IntegrationEventHandler<E> {
            myConnection.EnsureIsConnected();

            if (myChannel == null) {
                myChannel = myConnection.CreateChannel();
            }

            var theRoutingKey = typeof(E).Name;
            string theQueueName = $"q-{theRoutingKey}";

            myChannel.QueueDeclare(queue: theQueueName, durable: true, exclusive: false, autoDelete: false);

            var theConsumer = new EventingBasicConsumer(myChannel);
            theConsumer.Received += async (model, ea) => {
                var theMessage = Encoding.UTF8.GetString(ea.Body);
                E theEvent = JsonConvert.DeserializeObject<E>(theMessage);
                EH theHandler = (EH) myServiceProvider.GetService(typeof(EH));

                if (theHandler == null) {
                    throw new InvalidOperationException(
                        $"Service provider does not contain an implementation for {typeof(EH).FullName}."
                    );
                }

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
