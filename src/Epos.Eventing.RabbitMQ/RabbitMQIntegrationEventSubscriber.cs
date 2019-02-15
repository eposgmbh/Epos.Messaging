using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

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
        /// <param name="serviceProvider">Service provider to create <b>IntegrationCommandHandler</b> instances</param>
        /// <param name="connectionFactory">Connection factory</param>
        public RabbitMQIntegrationEventSubscriber(
            IServiceProvider serviceProvider, IConnectionFactory connectionFactory
        ) {
            myServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

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

            string theQueueName = $"q-{typeof(E).Name}-{Guid.NewGuid().ToString("N").ToLowerInvariant()}";
            string theExchangeName = $"e-{typeof(E).Name}";

            myChannel.QueueDeclare(theQueueName);
            myChannel.QueueBind(queue: theQueueName, exchange: theExchangeName, routingKey: string.Empty);

            var theConsumer = new EventingBasicConsumer(myChannel);
            theConsumer.Received += async (model, ea) => {
                string theMessage = Encoding.UTF8.GetString(ea.Body);
                E theCommand = JsonConvert.DeserializeObject<E>(theMessage);
                var theHandler = (EH) myServiceProvider.CreateScope().ServiceProvider.GetService(typeof(EH));

                if (theHandler == null) {
                    throw new InvalidOperationException(
                        $"Service provider does not contain an implementation for {typeof(EH).FullName}."
                    );
                }

                await theHandler.Handle(theCommand);
            };

            myChannel.BasicConsume(queue: theQueueName, autoAck: true, consumer: theConsumer);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() {
            myChannel.Dispose();
            myConnection.Dispose();
        }
    }
}
