using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Messaging.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationRequestPublisher : IIntegrationRequestPublisher
    {
        private readonly IConnection myConnection;
        private readonly ConcurrentDictionary<int, IChannel> myChannels;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationRequestPublisher</b> class. </summary>
        /// <param name="options">Eventing options</param>
        public RabbitMQIntegrationRequestPublisher(IOptions<RabbitMQOptions> options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            myConnection = PersistentConnection.Create(options.Value);
            myChannels = new ConcurrentDictionary<int, IChannel>();
        }

        /// <inheritdoc />
        public async Task<TReply> PublishAsync<TRequest, TReply>(TRequest request, int timeoutSeconds = 5)
            where TRequest : IntegrationRequest where TReply : IntegrationReply, new() {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            string theRequestReplyQueueName = $"q-{typeof(TRequest).Name}";

            if (!string.IsNullOrEmpty(request.Topic)) {
                theRequestReplyQueueName += $"-{request.Topic}";
            }

            IChannel theChannel = await myConnection.CreateChannelAsync().ConfigureAwait(false);

            QueueDeclareOk theQueueDeclareOk = await theChannel.QueueDeclareAsync().ConfigureAwait(false);

            string theReplyQueueName = theQueueDeclareOk.QueueName;

            var theConsumer = new AsyncEventingBasicConsumer(theChannel);

            var theProperties = new BasicProperties {
                ReplyTo = theReplyQueueName
            };

            var theTaskCompletionSource = new TaskCompletionSource<TReply>();

            theConsumer.ReceivedAsync += async (channel, args) => {
                string theReplyMessage = Encoding.UTF8.GetString(args.Body.ToArray());

                TReply theReply;
                try {
                    theReply = JsonSerializer.Deserialize<TReply>(theReplyMessage)!;
                    theTaskCompletionSource.SetResult(theReply);
                } catch (Exception theException) {
                    theTaskCompletionSource.SetException(theException);
                } finally {
                    await theChannel.QueueDeleteAsync(
                        queue: theReplyQueueName,
                        ifUnused: true,
                        ifEmpty: true
                    ).ConfigureAwait(false);

                    await theChannel.CloseAsync().ConfigureAwait(false);
                }
            };

            string theRequestMessage = JsonSerializer.Serialize(request);
            byte[] theRequestBody = Encoding.UTF8.GetBytes(theRequestMessage);

            await theChannel.BasicPublishAsync(
                exchange: Constants.DefaultExchangeName,
                routingKey: theRequestReplyQueueName,
                mandatory: true,
                basicProperties: theProperties,
                body: theRequestBody
            ).ConfigureAwait(false);

            await theChannel.BasicConsumeAsync(
                consumer: theConsumer,
                queue: theReplyQueueName,
                autoAck: true
            ).ConfigureAwait(false);

            var theTimeoutTask = Task.Delay(timeoutSeconds * 1000);
            Task theFirstCompletedTask =
                await Task.WhenAny(theTaskCompletionSource.Task, theTimeoutTask).ConfigureAwait(false);

            if (ReferenceEquals(theFirstCompletedTask, theTimeoutTask)) {
                throw new TimeoutException(
                    $"Timeout while waiting for a reply for an integration request: {theRequestMessage}"
                );
            }

            return await theTaskCompletionSource.Task.ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            if (myConnection.IsOpen) {
                await myConnection.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
