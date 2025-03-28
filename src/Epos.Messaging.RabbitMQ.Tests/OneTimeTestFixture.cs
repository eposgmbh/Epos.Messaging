using Epos.TestUtilities.Docker;
using NUnit.Framework;

namespace Epos.Messaging.RabbitMQ;

[SetUpFixture]
public class OneTimeTestFixture
{
    public static readonly DockerContainer RabbitMQContainer = DockerContainer.RabbitMQ.Start();

    [OneTimeTearDown]
    public void TearDown() => RabbitMQContainer.ForceRemove();
}
