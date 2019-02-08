namespace Epos.Eventing
{
    /// <summary> Base class for integration commands. </summary>
    /// <remarks> Integration commands must be handled by exactly one command handler and are durable in their
    /// respective queue. </remarks>
    public abstract class IntegrationCommand : IntegrationEventCommandBase
    {
    }
}
