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
        public Task<ISubscription> SubscribeAsync<TRequest, TReply>(string? topic = null)
            where TRequest : IntegrationRequest where TReply : IntegrationReply, new() {
            string theRequestReplyQueueName = $"q-{typeof(TRequest).Name}";
            if (!string.IsNullOrEmpty(topic)) {
                theRequestReplyQueueName += $"-{topic}";
            }

            IModel theChannel = myConnection.CreateModel();

            theChannel.QueueDeclare(
                queue: theRequestReplyQueueName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );
            theChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var theConsumer = new EventingBasicConsumer(theChannel);

            int theHandlerInProgressCount = 0;

            var theCancellationTokenSource = new CancellationTokenSource();

            var theSubscription = new Subscription();
            theSubscription.Cancelling += delegate {
                theCancellationTokenSource.Cancel();

                theChannel.BasicCancel(theConsumer.ConsumerTag);

                // Wait for handlers to finish
                while (theHandlerInProgressCount != 0) {
                    Thread.Sleep(Constants.WaitTimeout);
                }

                theChannel.Close();
            };

            theConsumer.Received += async (channel, args) => {
                if (theCancellationTokenSource.Token.IsCancellationRequested) {
                    return;
                }

                Interlocked.Increment(ref theHandlerInProgressCount);

                IBasicProperties theProperties = args.BasicProperties;

                IBasicProperties theReplyProps = theChannel.CreateBasicProperties();

                string theRequestMessage = Encoding.UTF8.GetString(args.Body);
                TRequest theRequest = JsonConvert.DeserializeObject<TRequest>(theRequestMessage);

                TReply theReply;
                using (IServiceScope theScope = myServiceProvider.CreateScope()) {
                    IIntegrationRequestHandler<TRequest, TReply> theHandler =
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

                string theReplyMessage = JsonConvert.SerializeObject(theReply);
                byte[] theBody = Encoding.UTF8.GetBytes(theReplyMessage);

                theChannel.BasicPublish(
                    exchange: Constants.DefaultExchangeName,
                    routingKey: theProperties.ReplyTo,
                    basicProperties: theReplyProps,
                    body: theBody
                );
            };

            theChannel.BasicConsume(
                queue: theRequestReplyQueueName,
                autoAck: true,
                consumer: theConsumer
            );

            return Task.FromResult((ISubscription) theSubscription);
        }

        /// <inheritdoc />
        public void Dispose() => myConnection.Close();
    }
}
