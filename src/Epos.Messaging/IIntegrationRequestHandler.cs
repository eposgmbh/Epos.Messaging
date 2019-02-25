using System.Threading;
using System.Threading.Tasks;

namespace Epos.Messaging
{
    /// <summary> Interface for request handlers. </summary>
    public interface IIntegrationRequestHandler<TRequest, TReply>
        where TRequest : IntegrationRequest where TReply : IntegrationReply {
        /// <summary>
        ///  Handles a request and returns a reply. </summary>
        /// <param name="request">Request</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Reply</returns>
        Task<TReply> Handle(TRequest request, CancellationToken token);
    }
}
