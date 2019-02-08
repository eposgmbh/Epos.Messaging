using System;
using System.Net.Sockets;

using Polly;
using Polly.Retry;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Epos.Eventing.RabbitMQ
{
    internal class PersistentConnection
    {
        private readonly object mySyncRoot = new object();

        private readonly IConnectionFactory myConnectionFactory;
        private readonly int myRetryCount;
        private IConnection myConnection;
        private bool myIsDisposed;

        public PersistentConnection(
            IConnectionFactory connectionFactory, int retryCount = 5
        ) {
            myConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            myRetryCount = retryCount;
        }

        public bool IsConnected => myConnection != null && myConnection.IsOpen && !myIsDisposed;

        public IModel CreateChannel() {
            if (!IsConnected) {
                throw new InvalidOperationException("No RabbitMQ connection is available to perform this action.");
            }

            return myConnection.CreateModel();
        }

        public void Dispose() {
            if (myIsDisposed) {
                return;
            }

            myIsDisposed = true;

            myConnection.Dispose();
        }

        public void EnsureIsConnected() {
            if (IsConnected) {
                return;
            }

            lock (mySyncRoot) {
                RetryPolicy thePolicy = RetryPolicy
                    .Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(
                        retryCount: myRetryCount,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    );

                thePolicy.Execute(() => myConnection = myConnectionFactory.CreateConnection());

                if (IsConnected) {
                    myConnection.ConnectionShutdown += OnConnectionShutdown;
                    myConnection.CallbackException += OnCallbackException;
                    myConnection.ConnectionBlocked += OnConnectionBlocked;
                } else {
                    throw new InvalidOperationException("RabbitMQ connection could not be created.");
                }
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e) {
            if (myIsDisposed) {
                return;
            }

            EnsureIsConnected();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e) {
            if (myIsDisposed) {
                return;
            }

            EnsureIsConnected();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason) {
            if (myIsDisposed) {
                return;
            }

            EnsureIsConnected();
        }
    }
}
