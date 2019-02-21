namespace Epos.Eventing
{
    /// <summary> Represents an integration command subscription. </summary>
    public interface ISubscription
    {
        /// <summary> Cancels the subscription and waits for finishing command handlers. </summary>
        void Cancel();
    }
}
