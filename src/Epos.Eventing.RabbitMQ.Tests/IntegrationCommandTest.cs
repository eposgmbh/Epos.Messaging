using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using RabbitMQ.Client;

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
        public void IntegrationCommands() {
            var thePublisher = new RabbitMQIntegrationCommandPublisher(
                new ConnectionFactory { AutomaticRecoveryEnabled = true, HostName = "localhost" }
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
                theServiceProvider, new ConnectionFactory { AutomaticRecoveryEnabled = true, HostName = "localhost" }
            );

            theSubscriber1.Subscribe<MyIntegrationCommand, MyIntegrationCommandHandler>(CancellationToken.None);

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
                theServiceProvider, new ConnectionFactory { AutomaticRecoveryEnabled = true, HostName = "localhost" }
            );

            theSubscriber2.Subscribe<MyIntegrationCommand, MyIntegrationCommandHandler>(CancellationToken.None);

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

        private class MyIntegrationCommand : IntegrationCommand
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

        private class ServiceProviderNumber
        {
            public ServiceProviderNumber(int number) {
                Number = number;
            }

            public int Number { get; }
        }
    }
}
