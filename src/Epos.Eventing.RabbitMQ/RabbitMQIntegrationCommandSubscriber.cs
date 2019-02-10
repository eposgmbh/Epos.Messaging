using System;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationCommandSubscriber : IIntegrationCommandSubscriber
    {
        private readonly IServiceProvider myServiceProvider;
        private readonly PersistentConnection myConnection;
        private IModel myChannel;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationCommandSubscriber</b> class. </summary>
        /// <param name="serviceProvider">Service provider to create <b>IntegrationCommandHandler</b> instances</param>
        /// <param name="connectionFactory">Connection factory</param>
        public RabbitMQIntegrationCommandSubscriber(
            IServiceProvider serviceProvider, IConnectionFactory connectionFactory
        ) {
            myServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            if (connectionFactory == null) {
                throw new ArgumentNullException(nameof(connectionFactory));
            }
            myConnection = new PersistentConnection(connectionFactory);
        }

        /// <inheritdoc />
        public Task Subscribe<C, CH>(string topic = null)
            where C : IntegrationCommand where CH : IntegrationCommandHandler<C> {
            myConnection.EnsureIsConnected();

            if (myChannel == null) {
                myChannel = myConnection.CreateChannel();
            }

            string theQueueName = $"q-{typeof(C).Name}";
            if (!string.IsNullOrEmpty(topic)) {
                theQueueName += $"-{topic}";
            }

            myChannel.QueueDeclare(queue: theQueueName, durable: true, exclusive: false, autoDelete: false);

            var theConsumer = new EventingBasicConsumer(myChannel);
            theConsumer.Received += async (model, ea) => {
                string theMessage = Encoding.UTF8.GetString(ea.Body);
                C theCommand = JsonConvert.DeserializeObject<C>(theMessage);
                var theHandler = (CH) myServiceProvider.GetService(typeof(CH));

                if (theHandler == null) {
                    throw new InvalidOperationException(
                        $"Service provider does not contain an implementation for {typeof(CH).FullName}."
                    );
                }

                await theHandler.Handle(
                    theCommand,
                    new MessagingHelper(
                        ack: () => myChannel.BasicAck(ea.DeliveryTag, multiple: false)
                    )
                );
            };

            myChannel.BasicConsume(queue: theQueueName, autoAck: false, consumer: theConsumer);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() => myConnection.Dispose();
    }
}
