using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Epos.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Messaging.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationCommandSubscriber : IIntegrationCommandSubscriber
    {
        private readonly IServiceProvider myServiceProvider;
        private readonly IConnection myConnection;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationCommandSubscriber</b> class. </summary>
        /// <param name="options">Eventing options</param>
        /// <param name="serviceProvider">Service provider to create <b>IntegrationCommandHandler</b> instances</param>
        public RabbitMQIntegrationCommandSubscriber(
            IOptions<RabbitMQOptions> options,
            IServiceProvider serviceProvider
        ) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            myServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            myConnection = PersistentConnection.Create(options.Value);
        }

        /// <inheritdoc />
        public async Task<ISubscription> SubscribeAsync<C>(string? topic = null) where C : IntegrationCommand {
            string theQueueName = $"q-{typeof(C).Name}";
            if (!string.IsNullOrEmpty(topic)) {
                theQueueName += $"-{topic}";
            }

            IChannel theChannel = await myConnection.CreateChannelAsync().ConfigureAwait(false);

            await theChannel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: (ushort) Environment.ProcessorCount,
                global: false
            ).ConfigureAwait(false);

            await theChannel.QueueDeclareAsync(
                queue: theQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            ).ConfigureAwait(false);

            var theConsumer = new AsyncEventingBasicConsumer(theChannel);
            int theHandlerInProgressCount = 0;

            var theCancellationTokenSource = new CancellationTokenSource();

            var theSubscription = new Subscription();
            theSubscription.CancellingAsync += async delegate {
                theCancellationTokenSource.Cancel();

                if (myConnection.IsOpen) {
                    await theChannel.BasicCancelAsync(theConsumer.ConsumerTags[0]).ConfigureAwait(false);
                }

                // Wait for handlers to finish
                while (theHandlerInProgressCount != 0) {
                    Thread.Sleep(Constants.WaitTimeout);
                }

                await theChannel.CloseAsync().ConfigureAwait(false);
            };

            theConsumer.ReceivedAsync += async (model, args) => {
                if (theCancellationTokenSource.Token.IsCancellationRequested) {
                    return;
                }

                Interlocked.Increment(ref theHandlerInProgressCount);

                string theMessage = Encoding.UTF8.GetString(args.Body.ToArray());
                C theCommand = JsonSerializer.Deserialize<C>(theMessage)!;

                using IServiceScope theScope = myServiceProvider.CreateScope();
                IIntegrationCommandHandler<C>? theHandler =
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
                        new IntegrationCommandHelper(
                            ack: async () => {
                                await theChannel.BasicAckAsync(args.DeliveryTag, multiple: false).ConfigureAwait(false);
                            }
                        )
                    ).ConfigureAwait(false);
                } finally {
                    Interlocked.Decrement(ref theHandlerInProgressCount);
                }
            };

            await theChannel.BasicConsumeAsync(
                queue: theQueueName,
                autoAck: false,
                consumer: theConsumer
            ).ConfigureAwait(false);

            return theSubscription;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            if (myConnection.IsOpen) {
                await myConnection.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
