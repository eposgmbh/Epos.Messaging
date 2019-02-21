using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Epos.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Eventing.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationCommandSubscriber : IIntegrationCommandSubscriber
    {
        private const int WaitTimeout = 250;

        private readonly IServiceProvider myServiceProvider;
        private readonly IConnection myConnection;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationCommandSubscriber</b> class. </summary>
        /// <param name="options">Eventing options</param>
        /// <param name="serviceProvider">Service provider to create <b>IntegrationCommandHandler</b> instances</param>
        public RabbitMQIntegrationCommandSubscriber(
            IOptions<EventingOptions> options,
            IServiceProvider serviceProvider
        ) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            myServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            myConnection = PersistentConnection.Create(options.Value);
        }

        /// <inheritdoc />
        public Task<ISubscription> SubscribeAsync<C>(string topic = null) where C : IntegrationCommand {
            string theQueueName = $"q-{typeof(C).Name}";
            if (!string.IsNullOrEmpty(topic)) {
                theQueueName += $"-{topic}";
            }

            IModel theChannel = myConnection.CreateModel();
            theChannel.QueueDeclare(queue: theQueueName, durable: true, exclusive: false, autoDelete: false);

            var theConsumer = new EventingBasicConsumer(theChannel);
            int theHandlerInProgressCount = 0;

            var theCancellationTokenSource = new CancellationTokenSource();

            var theSubscription = new Subscription();
            theSubscription.Cancelling += delegate {
                theCancellationTokenSource.Cancel();

                theChannel.BasicCancel(theConsumer.ConsumerTag);

                // Wait for handlers to finish
                while (theHandlerInProgressCount != 0) {
                    Thread.Sleep(WaitTimeout);
                }

                theChannel.Close();
            };

            theConsumer.Received += async (model, ea) => {
                if (theCancellationTokenSource.Token.IsCancellationRequested) {
                    return;
                }

                Interlocked.Increment(ref theHandlerInProgressCount);

                string theMessage = Encoding.UTF8.GetString(ea.Body);
                C theCommand = JsonConvert.DeserializeObject<C>(theMessage);

                using (IServiceScope theScope = myServiceProvider.CreateScope()) {
                    IIntegrationCommandHandler<C> theHandler =
                        theScope.ServiceProvider.GetService<IIntegrationCommandHandler<C>>();

                    try {
                        if (theHandler == null) {
                            throw new InvalidOperationException(
                                "The service provider does not contain an implementation for " +
                                typeof(IIntegrationCommandHandler<C>).Dump() + "."
                            );
                        }

                        await theHandler.Handle(
                            theCommand,
                            theCancellationTokenSource.Token,
                            new CommandHelper(
                                ack: () => {
                                    theChannel.BasicAck(ea.DeliveryTag, multiple: false);
                                    return Task.CompletedTask;
                                }
                            )
                        );
                    } finally {
                        Interlocked.Decrement(ref theHandlerInProgressCount);
                    }
                }
            };

            theChannel.BasicConsume(queue: theQueueName, autoAck: false, consumer: theConsumer);

            return Task.FromResult((ISubscription) theSubscription);
        }

        /// <inheritdoc />
        public void Dispose() => myConnection.Close();
    }
}
