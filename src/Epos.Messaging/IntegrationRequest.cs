namespace Epos.Messaging
{
    /// <summary> Base class for integration requests. </summary>
    /// <remarks> Integration requests must be handled by exactly one request handler and are transient. </remarks>
    public abstract class IntegrationRequest : MessageBase
    {
        /// <summary> Gets or sets a topic to further differentiate the integration request. </summary>
        public string? Topic { get; set; }
    }
}
