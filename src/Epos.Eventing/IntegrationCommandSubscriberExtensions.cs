using System.Threading;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Extension methods for the <b>IIntegrationCommandSubscriber</b> interface. </summary>
    public static class IntegrationCommandSubscriberExtensions
    {
        /// <summary> Subscribes an integration command and registers its command handler. </summary>
        /// <returns>Subscription</returns>
        /// <typeparam name="C">Integration command class</typeparam>
        /// <param name="subscriber">Integration command subscriber</param>
        public static Task<ISubscription> SubscribeAsync<C>(this IIntegrationCommandSubscriber subscriber)
            where C : IntegrationCommand => subscriber.SubscribeAsync<C>(topic: null);
    }
}
