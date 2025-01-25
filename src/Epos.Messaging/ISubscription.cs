using System;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Represents an integration command subscription. </summary>
    public interface ISubscription : IAsyncDisposable
    {
        /// <summary> Cancels the subscription and waits for finishing command handlers. </summary>
        ValueTask CancelAsync();
    }
}
