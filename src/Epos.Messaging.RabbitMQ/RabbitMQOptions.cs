namespace Epos.Messaging.RabbitMQ
{
    /// <summary> Contains options for RabbitMQ messaging like Hostname. </summary>
    public sealed class RabbitMQOptions
    {
        /// <summary> Default options for localhost. </summary>
        public static readonly RabbitMQOptions Default = new();

        /// <summary> Gets or sets the connection string. </summary>
        public string ConnectionString { get; init; } = "amqp://guest:guest@localhost:5672";
    }
}
