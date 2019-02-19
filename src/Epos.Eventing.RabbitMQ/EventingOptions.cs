namespace Epos.Eventing.RabbitMQ
{
    /// <summary> Contains options for RabbitMQ eventing like Hostname. </summary>
    public sealed class EventingOptions
    {
        /// <summary> Default options for localhost. </summary>
        public static readonly EventingOptions Default = new EventingOptions();

        /// <summary> Gets or sets the hostname (default: localhost). </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary> Gets or sets the username (default: guest). </summary>
        public string Username { get; set; } = "guest";

        /// <summary> Gets or sets the password (default: guest). </summary>
        public string Password { get; set; } = "guest";
    }
}
