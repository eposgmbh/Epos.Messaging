using System;

namespace Epos.Messaging
{
    /// <summary> Represents an integration command subscription. </summary>
    public interface ISubscription : IDisposable
    {
        /// <summary> Cancels the subscription and waits for finishing command handlers. </summary>
        void Cancel();
    }
}
