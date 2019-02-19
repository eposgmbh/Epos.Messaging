using Epos.TestUtilities.Docker;

namespace Epos.Eventing.RabbitMQ
{
    public static class RabbitMQContainer
    {
        private static DockerContainer myContainer;

        public static void Start() {
            var theContainerOptions = new DockerContainerOptions {
                ImageName = "rabbitmq:3.7.4-management-alpine",
                Hostname = "rabbitmq-host",
                Ports = {
                    (hostPort: 5672, containerPort: 5672),
                    (hostPort: 15672, containerPort: 15672)
                },
                ReadynessLogPhrase = "Server startup complete"
            };

            myContainer = DockerContainer.StartAndWaitForReadynessLogPhrase(theContainerOptions);
        }

        public static void ForceRemove() => myContainer?.ForceRemove();
    }
}
