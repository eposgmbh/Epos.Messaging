namespace Epos.Eventing
{
    /// <summary> Base class for integration commands. </summary>
    /// <remarks> Integration commands must be handled by exactly one command handler and are durable in their
    /// respective queue. </remarks>
    public abstract class IntegrationCommand : IntegrationEventCommandBase
    {
        /// <summary> Gets or sets a topic to further differentiate the integration command. </summary>
        public string Topic { get; set; }
    }
}
