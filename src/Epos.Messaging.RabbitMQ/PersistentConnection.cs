using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Epos.Messaging.RabbitMQ
{
    internal static class PersistentConnection
    {
        private const int ConnectionFactoryRetryCount = 5;

        public static IConnection Create(RabbitMQOptions options) {
            var theConnectionFactory = new ConnectionFactory {
                AutomaticRecoveryEnabled = true,
                ConsumerDispatchConcurrency = (ushort) Environment.ProcessorCount,
                HostName = options.Hostname,
                UserName = options.Username,
                Password = options.Password
            };

            RetryPolicy thePolicy = Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(
                    retryCount: ConnectionFactoryRetryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            IConnection? theConnection = null;
            thePolicy.Execute(() => theConnection = theConnectionFactory.CreateConnectionAsync().Result);

            if (theConnection == null || !theConnection.IsOpen) {
                throw new InvalidOperationException("RabbitMQ connection could not be created.");
            }

            return theConnection;
        }
    }
}
