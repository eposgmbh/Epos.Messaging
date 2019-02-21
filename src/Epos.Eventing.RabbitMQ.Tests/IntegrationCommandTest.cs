using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NUnit.Framework;

namespace Epos.Eventing.RabbitMQ
{
    [TestFixture]
    public class IntegrationCommandTest
    {
        [SetUp]
        public void InitContainer() => RabbitMQContainer.Start();

        [TearDown]
        public void RemoveContainer() => RabbitMQContainer.ForceRemove();

        [Test]
        public async Task IntegrationCommands() {
            var theOptions = new OptionsWrapper<EventingOptions>(EventingOptions.Default);

            var thePublisher = new RabbitMQIntegrationCommandPublisher(
                theOptions
            );

            // Ein Command senden
            await thePublisher.PublishAsync(new MyIntegrationCommand { Payload = "C1" });

            // ---

            // Erst jetzt Command handler registrieren
            var theServiceCollection = new ServiceCollection();
            theServiceCollection.AddSingleton(new ServiceProviderNumber(1));
            theServiceCollection.AddIntegrationCommandHandler(typeof(MyIntegrationCommandHandler));
            ServiceProvider theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber1 = new RabbitMQIntegrationCommandSubscriber(theOptions, theServiceProvider);

            ISubscription theSubscription1 = await theSubscriber1.SubscribeAsync<MyIntegrationCommand>();

            Thread.Sleep(1000);

            // Sicherstellen, dass der Handler das obige Command handelt
            Assert.That(MyIntegrationCommandHandler.Payloads, Has.Exactly(1).EqualTo("1: C1"));

            // ---

            // Zweiten Command handler registrieren
            theServiceCollection = new ServiceCollection();
            theServiceCollection.AddSingleton(new ServiceProviderNumber(2));
            theServiceCollection.AddIntegrationCommandHandler(typeof(MyIntegrationCommandHandler));
            theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber2 = new RabbitMQIntegrationCommandSubscriber(theOptions, theServiceProvider);

            ISubscription theSubscription2 = await theSubscriber2.SubscribeAsync<MyIntegrationCommand>();

            // Es wurde nach wie vor nur ein Command gehandelt
            Assert.That(MyIntegrationCommandHandler.Payloads, Has.Exactly(1).EqualTo("1: C1"));

            // ---

            // Vier Commands im Abstand von einer Sekunde losschicken
            await thePublisher.PublishAsync(new MyIntegrationCommand { Payload = "C2" });
            Thread.Sleep(1000);
            await thePublisher.PublishAsync(new MyIntegrationCommand { Payload = "C3" });
            Thread.Sleep(1000);
            await thePublisher.PublishAsync(new MyIntegrationCommand { Payload = "C4" });
            Thread.Sleep(1000);
            await thePublisher.PublishAsync(new MyIntegrationCommand { Payload = "C5" });
            Thread.Sleep(1000);

            // ---

            // Jetzt sicherstellen, dass alle Commands von den entsprechenden Handlern bearbeitet wurden
            Assert.That(MyIntegrationCommandHandler.Payloads, Has.Count.EqualTo(5));
            Assert.That(MyIntegrationCommandHandler.Payloads, Contains.Item("1: C1"));
            Assert.That(MyIntegrationCommandHandler.Payloads, Contains.Item("1: C2"));
            Assert.That(MyIntegrationCommandHandler.Payloads, Contains.Item("2: C3"));
            Assert.That(MyIntegrationCommandHandler.Payloads, Contains.Item("1: C4"));
            Assert.That(MyIntegrationCommandHandler.Payloads, Contains.Item("2: C5"));

            // ---

            theSubscription1.Cancel();
            theSubscription2.Cancel();

            thePublisher.Dispose();
            theSubscriber1.Dispose();
            theSubscriber2.Dispose();
        }

        private class MyIntegrationCommand : IntegrationCommand
        {
            public string Payload { get; set; }
        }

        private class MyIntegrationCommandHandler : IIntegrationCommandHandler<MyIntegrationCommand>
        {
            public static readonly ConcurrentBag<string> Payloads = new ConcurrentBag<string>();

            private readonly ServiceProviderNumber myNumber;

            public MyIntegrationCommandHandler(ServiceProviderNumber number) {
                myNumber = number;
            }

            public Task Handle(MyIntegrationCommand c, CancellationToken token, CommandHelper h) {
                Payloads.Add($"{myNumber.Number}: {c.Payload}");
                h.Ack();

                return Task.CompletedTask;
            }
        }

        private class ServiceProviderNumber
        {
            public ServiceProviderNumber(int number) {
                Number = number;
            }

            public int Number { get; }
        }
    }
}
