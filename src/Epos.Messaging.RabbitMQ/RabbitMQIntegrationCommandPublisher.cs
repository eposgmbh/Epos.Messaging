using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using RabbitMQ.Client;

namespace Epos.Messaging.RabbitMQ
{
    /// <inheritdoc />
    public class RabbitMQIntegrationCommandPublisher : IIntegrationCommandPublisher {
        private readonly IConnection myConnection;
        private readonly ConcurrentDictionary<int, IModel> myChannels;

        /// <summary> Creates an instance of the <b>RabbitMQIntegrationCommandPublisher</b> class. </summary>
        /// <param name="options">Eventing options</param>
        public RabbitMQIntegrationCommandPublisher(IOptions<RabbitMQOptions> options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            myConnection = PersistentConnection.Create(options.Value);
            myChannels = new ConcurrentDictionary<int, IModel>();
        }

        /// <inheritdoc />
        public Task PublishAsync<C>(C c) where C : IntegrationCommand {
            if (c == null) {
                throw new ArgumentNullException(nameof(c));
            }

            string theRoutingKey = $"q-{c.GetType().Name}";

            if (!string.IsNullOrEmpty(c.Topic)) {
                theRoutingKey += $"-{c.Topic}";
            }

            IModel theChannel = GetChannel();
            theChannel.QueueDeclare(queue: theRoutingKey, durable: true, exclusive: false, autoDelete: false);

            string theMessage = JsonConvert.SerializeObject(c);
            byte[] theBody = Encoding.UTF8.GetBytes(theMessage);

            IBasicProperties theProperties = theChannel.CreateBasicProperties();
            theProperties.Persistent = true;

            theChannel.BasicPublish(
                exchange: Constants.DefaultExchangeName,
                routingKey: theRoutingKey,
                basicProperties: theProperties,
                body: theBody
            );

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose() {
            if (myConnection.IsOpen) {
                myConnection.Close();
            }
        }

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
