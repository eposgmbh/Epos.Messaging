using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NUnit.Framework;

namespace Epos.Messaging.RabbitMQ
{
    [TestFixture]
    public class IntegrationRequestTest
    {
        [Test]
        public async Task RequestAndReplies() {
            IOptions<RabbitMQOptions> theOptions = Options.Create(new RabbitMQOptions {
                ConnectionString = OneTimeTestFixture.RabbitMQContainer.ConnectionString
            });

            var thePublisher = new RabbitMQIntegrationRequestPublisher(theOptions);

            // Request handler registrieren
            var theServiceCollection = new ServiceCollection();
            theServiceCollection.AddIntegrationRequestHandler(typeof(MyRequestHandler));
            ServiceProvider theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber = new RabbitMQIntegrationRequestSubscriber(theOptions, theServiceProvider);

            ISubscription theSubscription = await theSubscriber.SubscribeAsync<MyRequest, MyReply>(topic: "MyTopic");

            // Einen Request senden
            MyReply theReply =
                await thePublisher.PublishAsync<MyRequest, MyReply>(new MyRequest { Topic = "MyTopic", Number = 5 });
            Assert.That(theReply.DoubledNumber, Is.EqualTo(10));

            await theSubscription.CancelAsync();
            await theSubscriber.DisposeAsync();

            await thePublisher.DisposeAsync();
        }

        [Test]
        public async Task Timeout() {
            var theOptions = new OptionsWrapper<RabbitMQOptions>(new RabbitMQOptions {
                ConnectionString = OneTimeTestFixture.RabbitMQContainer.ConnectionString
            });

            var thePublisher = new RabbitMQIntegrationRequestPublisher(theOptions);

            // Request handler registrieren
            var theServiceCollection = new ServiceCollection();
            theServiceCollection.AddIntegrationRequestHandler(typeof(MyDelayRequestHandler));
            ServiceProvider theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber = new RabbitMQIntegrationRequestSubscriber(theOptions, theServiceProvider);

            ISubscription theSubscription = await theSubscriber.SubscribeAsync<MyRequest, MyReply>();

            // Einen Request senden
            Assert.ThrowsAsync<TimeoutException>(async () => {
                await thePublisher.PublishAsync<MyRequest, MyReply>(new MyRequest { Number = 5 }, timeoutSeconds: 1);
            });

            await theSubscription.CancelAsync();
            await theSubscriber.DisposeAsync();

            await thePublisher.DisposeAsync();
        }

        private class MyRequest : IntegrationRequest
        {
            public int Number { get; set; }
        }

        private class MyReply : IntegrationReply
        {
            public int DoubledNumber { get; set; }
        }

        private class MyRequestHandler : IIntegrationRequestHandler<MyRequest, MyReply>
        {
            public virtual Task<MyReply> Handle(MyRequest request, CancellationToken token) =>
                Task.FromResult(new MyReply { DoubledNumber = request.Number * 2 });
        }

        private class MyDelayRequestHandler : MyRequestHandler
        {
            public override async Task<MyReply> Handle(MyRequest request, CancellationToken token) {
                await Task.Delay(5000);
                return await base.Handle(request, token);
            }
        }
    }
}
