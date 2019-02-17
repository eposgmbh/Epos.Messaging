using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationCommandSubscriber : IIntegrationCommandSubscriber
    {
        private readonly IServiceProvider myServiceProvider;
        private readonly IConnection myConnection;
        private readonly IModel myChannel;

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
            myConnection = connectionFactory.CreateConnection();
            myChannel = myConnection.CreateModel();
        }

        /// <inheritdoc />
        public Task Subscribe<C, CH>(CancellationToken token, string topic = null)
            where C : IntegrationCommand where CH : IntegrationCommandHandler<C> {
            string theQueueName = $"q-{typeof(C).Name}";
            if (!string.IsNullOrEmpty(topic)) {
                theQueueName += $"-{topic}";
            }

            myChannel.QueueDeclare(queue: theQueueName, durable: true, exclusive: false, autoDelete: false);

            var theConsumer = new EventingBasicConsumer(myChannel);

            token.Register(() => myChannel.BasicCancel(theConsumer.ConsumerTag));

            theConsumer.Received += async (model, ea) => {
                string theMessage = Encoding.UTF8.GetString(ea.Body);
                C theCommand = JsonConvert.DeserializeObject<C>(theMessage);
                var theHandler = (CH) myServiceProvider.CreateScope().ServiceProvider.GetService(typeof(CH));

                if (theHandler == null) {
                    throw new InvalidOperationException(
                        $"The service provider does not contain an implementation for {typeof(CH).FullName}."
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
        public void Dispose() {
            myChannel.Dispose();
            myConnection.Dispose();
        }
    }
}
