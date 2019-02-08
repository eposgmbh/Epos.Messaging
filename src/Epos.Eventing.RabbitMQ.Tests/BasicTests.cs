using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Epos.TestUtilities.Docker;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using RabbitMQ.Client;

namespace Epos.Eventing.RabbitMQ
{
    [TestFixture]
    public class Tests
    {
        private DockerContainer myRabbitMQContainer;

        [OneTimeSetUp]
        public void StartRabbitMQContainer() {
            var theContainerOptions = new DockerContainerOptions {
                ImageName = "rabbitmq:3.7.4-management-alpine",
                Hostname = "rabbitmq-host",
                Ports = {
                    (hostPort: 5672, containerPort: 5672),
                    (hostPort: 15672, containerPort: 15672)
                },
                ReadynessLogPhrase = "Server startup complete"
            };

            myRabbitMQContainer = DockerContainer.StartAndWaitForReadynessLogPhrase(theContainerOptions);
        }

        [OneTimeTearDown]
        public void ForceRemoveRabbitMQContainer() => myRabbitMQContainer.ForceRemove();

        [Test]
        public void Commands() {
            var thePublisher = new RabbitMQIntegrationCommandPublisher(
                new ConnectionFactory { HostName = "localhost" }
            );

            // Ein Command senden
            thePublisher.Publish(new MyIntegrationCommand { Payload = "C1" });

            // ---

            // Erst jetzt Command handler registrieren
            var theServiceCollection = new ServiceCollection();
            theServiceCollection.AddSingleton(new ServiceProviderNumber(1));
            theServiceCollection.AddTransient<MyIntegrationCommandHandler>();
            ServiceProvider theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber1 = new RabbitMQIntegrationCommandSubscriber(
                theServiceProvider, new ConnectionFactory { HostName = "localhost" }
            );

            theSubscriber1.Subscribe<MyIntegrationCommand, MyIntegrationCommandHandler>();

            Thread.Sleep(1000);

            // Sicherstellen, dass der Handler das obige Command handelt
            Assert.That(MyIntegrationCommandHandler.Payloads, Has.Exactly(1).EqualTo("1: C1"));

            // ---

            // Zweiten Command handler registrieren
            theServiceCollection = new ServiceCollection();
            theServiceCollection.AddSingleton(new ServiceProviderNumber(2));
            theServiceCollection.AddTransient<MyIntegrationCommandHandler>();
            theServiceProvider = theServiceCollection.BuildServiceProvider();

            var theSubscriber2 = new RabbitMQIntegrationCommandSubscriber(
                theServiceProvider, new ConnectionFactory { HostName = "localhost" }
            );

            theSubscriber2.Subscribe<MyIntegrationCommand, MyIntegrationCommandHandler>();

            // Es wurde nach wie vor nur ein Command gehandelt
            Assert.That(MyIntegrationCommandHandler.Payloads, Has.Exactly(1).EqualTo("1: C1"));

            // ---

            // Vier Commands im Abstand von einer Sekunde losschicken
            thePublisher.Publish(new MyIntegrationCommand { Payload = "C2" });
            Thread.Sleep(1000);
            thePublisher.Publish(new MyIntegrationCommand { Payload = "C3" });
            Thread.Sleep(1000);
            thePublisher.Publish(new MyIntegrationCommand { Payload = "C4" });
            Thread.Sleep(1000);
            thePublisher.Publish(new MyIntegrationCommand { Payload = "C5" });
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

            thePublisher.Dispose();
            theSubscriber1.Dispose();
            theSubscriber2.Dispose();
        }

        [Test]
        public void Events() {
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

        private class MyIntegrationCommand : IntegrationCommand
        {
            public string Payload { get; set; }
        }

        private class MyIntegrationEvent : IntegrationEvent
        {
            public string Payload { get; set; }
        }

        private class MyIntegrationCommandHandler : IntegrationCommandHandler<MyIntegrationCommand>
        {
            public static readonly ConcurrentBag<string> Payloads = new ConcurrentBag<string>();

            private readonly ServiceProviderNumber myNumber;

            public MyIntegrationCommandHandler(ServiceProviderNumber number) {
                myNumber = number;
            }

            public override Task Handle(MyIntegrationCommand c, MessagingHelper h) {
                Payloads.Add($"{myNumber.Number}: {c.Payload}");
                h.Ack();

                return Task.CompletedTask;
            }
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
