using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using RabbitMQ.Client;

namespace Epos.Eventing.RabbitMQ
{
    [TestFixture]
    public class IntegrationEventTest
    {
        [SetUp]
        public void InitContainer() => RabbitMQContainer.Start();

        [TearDown]
        public void RemoveContainer() => RabbitMQContainer.ForceRemove();

        [Test]
        public void IntegrationEvents() {
            var thePublisher = new RabbitMQIntegrationEventPublisher(
                new ConnectionFactory { HostName = "localhost" }
            );

            // Ein Event senden
            thePublisher.Publish(new MyIntegrationEvent { Payload = "E1" });

            // ---

            // Erst jetzt Event handler registrieren
            var theServiceCollection = new ServiceCollection();
            theServiceCollection.AddSingleton(new ServiceProviderNumber(1));
            theServiceCollection.AddTransient<MyIntegrationEventHandler>();
            ServiceProvider theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber1 = new RabbitMQIntegrationEventSubscriber(
                theServiceProvider, new ConnectionFactory { HostName = "localhost" }
            );

            theSubscriber1.Subscribe<MyIntegrationEvent, MyIntegrationEventHandler>();

            Thread.Sleep(1000);

            // Sicherstellen, dass der Handler das obige Event nicht gehandelt hat
            Assert.That(MyIntegrationEventHandler.Payloads, Has.Count.EqualTo(0));

            // ---

            // Zweiten Event handler registrieren
            theServiceCollection = new ServiceCollection();
            theServiceCollection.AddSingleton(new ServiceProviderNumber(2));
            theServiceCollection.AddTransient<MyIntegrationEventHandler>();
            theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber2 = new RabbitMQIntegrationEventSubscriber(
                theServiceProvider, new ConnectionFactory { HostName = "localhost" }
            );

            theSubscriber2.Subscribe<MyIntegrationEvent, MyIntegrationEventHandler>();

            // Es wurde nach wie vor kein Event gehandelt
            Assert.That(MyIntegrationEventHandler.Payloads, Has.Count.EqualTo(0));

            // ---

            // Ein Event losschicken
            thePublisher.Publish(new MyIntegrationEvent { Payload = "E2" });
            Thread.Sleep(1000);

            // ---

            // Jetzt sicherstellen, dass das Event von allen Handlern bearbeitet wurde
            Assert.That(MyIntegrationEventHandler.Payloads, Has.Count.EqualTo(2));
            Assert.That(MyIntegrationEventHandler.Payloads, Contains.Item("1: E2"));
            Assert.That(MyIntegrationEventHandler.Payloads, Contains.Item("2: E2"));

            // ---

            thePublisher.Dispose();
            theSubscriber1.Dispose();
            theSubscriber2.Dispose();
        }

        private class MyIntegrationEvent : IntegrationEvent
        {
            public string Payload { get; set; }
        }

        private class MyIntegrationEventHandler : IntegrationEventHandler<MyIntegrationEvent>
        {
            public static readonly ConcurrentBag<string> Payloads = new ConcurrentBag<string>();

            private readonly ServiceProviderNumber myNumber;

            public MyIntegrationEventHandler(ServiceProviderNumber number) {
                myNumber = number;
            }

            public override Task Handle(MyIntegrationEvent e) {
                Payloads.Add($"{myNumber.Number}: {e.Payload}");

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
