using Epos.TestUtilities.Docker;

using NUnit.Framework;

namespace Epos.Eventing.RabbitMQ
{
    [SetUpFixture]
    public class SetupFixture
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
    }
}
