namespace Epos.Eventing
{
    /// <summary> Base class for integration events. </summary>
    /// <remarks> Integration commands can be handled by more then one event handler and are not durable in their
    /// respective queue. They are automatically acknowledged. </remarks>
    public abstract class IntegrationEvent : IntegrationEventCommandBase
    {
    }
}
