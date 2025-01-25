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
    public class RabbitMQIntegrationRequestSubscriber : IIntegrationRequestSubscriber
    {
        private readonly IServiceProvider myServiceProvider;
        private readonly IConnection myConnection;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationRequestSubscriber</b> class. </summary>
        /// <param name="options">Eventing options</param>
        /// <param name="serviceProvider">Service provider to create
        /// <see cref="IIntegrationRequestHandler{TRequest, TReply}" /> instances</param>
        public RabbitMQIntegrationRequestSubscriber(
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
        public async Task<ISubscription> SubscribeAsync<TRequest, TReply>(string? topic = null)
            where TRequest : IntegrationRequest where TReply : IntegrationReply, new() {
            string theRequestReplyQueueName = $"q-{typeof(TRequest).Name}";
            if (!string.IsNullOrEmpty(topic)) {
                theRequestReplyQueueName += $"-{topic}";
            }

            IChannel theChannel = await myConnection.CreateChannelAsync().ConfigureAwait(false);

            await theChannel.QueueDeclareAsync(
                queue: theRequestReplyQueueName,
                durable: false,
                exclusive: false,
                autoDelete: false
            ).ConfigureAwait(false);

            await theChannel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: (ushort) Environment.ProcessorCount,
                global: false
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

            theConsumer.ReceivedAsync += async (channel, args) => {
                if (theCancellationTokenSource.Token.IsCancellationRequested) {
                    return;
                }

                Interlocked.Increment(ref theHandlerInProgressCount);

                IReadOnlyBasicProperties theProperties = args.BasicProperties;

                var theReplyProps = new BasicProperties();

                string theRequestMessage = Encoding.UTF8.GetString(args.Body.ToArray());
                TRequest theRequest = JsonSerializer.Deserialize<TRequest>(theRequestMessage)!;

                TReply theReply;
                using (IServiceScope theScope = myServiceProvider.CreateScope()) {
                    IIntegrationRequestHandler<TRequest, TReply>? theHandler =
                        theScope.ServiceProvider.GetService<IIntegrationRequestHandler<TRequest, TReply>>();

                    try {
                        if (theHandler == null) {
                            throw new InvalidOperationException(
                                "The service provider does not contain an implementation for " +
                                typeof(IIntegrationRequestHandler<TRequest, TReply>).Dump() + "."
                            );
                        }

                        theReply = await theHandler.Handle(
                            theRequest,
                            theCancellationTokenSource.Token
                        ).ConfigureAwait(false);
                    } catch (Exception theException) {
                        theReply = new TReply { ErrorMessage = theException.ToString() };
                    } finally {
                        Interlocked.Decrement(ref theHandlerInProgressCount);
                    }
                }

                string theReplyMessage = JsonSerializer.Serialize(theReply);
                byte[] theBody = Encoding.UTF8.GetBytes(theReplyMessage);

                await theChannel.BasicPublishAsync(
                    exchange: Constants.DefaultExchangeName,
                    routingKey: theProperties.ReplyTo ?? throw new InvalidOperationException("ReplyTo cannot be null."),
                    mandatory: true,
                    basicProperties: theReplyProps,
                    body: theBody
                ).ConfigureAwait(false);
            };

            await theChannel.BasicConsumeAsync(
                queue: theRequestReplyQueueName,
                autoAck: true,
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
