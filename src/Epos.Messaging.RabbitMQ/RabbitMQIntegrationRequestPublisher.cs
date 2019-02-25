using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Epos.Messaging.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationRequestPublisher : IIntegrationRequestPublisher
    {
        private readonly IConnection myConnection;
        private readonly ConcurrentDictionary<int, IModel> myChannels;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationRequestPublisher</b> class. </summary>
        /// <param name="options">Eventing options</param>
        public RabbitMQIntegrationRequestPublisher(IOptions<RabbitMQOptions> options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            myConnection = PersistentConnection.Create(options.Value);
            myChannels = new ConcurrentDictionary<int, IModel>();
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

            IModel theChannel = GetChannel();

            string theReplyQueueName = theChannel.QueueDeclare().QueueName;
            var theConsumer = new EventingBasicConsumer(theChannel);

            IBasicProperties theProperties = theChannel.CreateBasicProperties();
            theProperties.ReplyTo = theReplyQueueName;

            var theTaskCompletionSource = new TaskCompletionSource<TReply>();

            theConsumer.Received += (channel, args) => {
                string theReplyMessage = Encoding.UTF8.GetString(args.Body);

                TReply theReply;
                try {
                    theReply = JsonConvert.DeserializeObject<TReply>(theReplyMessage);
                    theTaskCompletionSource.SetResult(theReply);
                } catch (Exception theException) {
                    theTaskCompletionSource.SetException(theException);
                }
            };

            string theRequestMessage = JsonConvert.SerializeObject(request);
            byte[] theRequestBody = Encoding.UTF8.GetBytes(theRequestMessage);

            theChannel.BasicPublish(
                exchange: Constants.DefaultExchangeName,
                routingKey: theRequestReplyQueueName,
                basicProperties: theProperties,
                body: theRequestBody
            );

            theChannel.BasicConsume(
                consumer: theConsumer,
                queue: theReplyQueueName,
                autoAck: true
            );

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
        public void Dispose() => myConnection.Close();

        #region --- Hilfsmethoden ---

        private IModel GetChannel() {
            int theThreadId = Thread.CurrentThread.ManagedThreadId;

            if (!myChannels.TryGetValue(theThreadId, out IModel theResult)) {
                theResult = myConnection.CreateModel();
                myChannels[theThreadId] = theResult;
            }

            return theResult;
        }

        #endregion
    }
}
