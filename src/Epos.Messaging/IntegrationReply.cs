namespace Epos.Messaging
{
    /// <summary> Base class for integration replies. </summary>
    public abstract class IntegrationReply : MessageBase
    {
        /// <summary> Gets or sets an optional error message. </summary>
        public string? ErrorMessage { get; set; }
    }
}
