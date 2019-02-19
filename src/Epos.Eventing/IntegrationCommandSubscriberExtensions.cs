using System.Threading;
using System.Threading.Tasks;

namespace Epos.Eventing
{
    /// <summary> Extension methods for the <b>IIntegrationCommandSubscriber</b> interface. </summary>
    public static class IntegrationCommandSubscriberExtensions
    {
        /// <summary> Subscribes an integration command and registers its command handler. </summary>
        /// <returns>Task</returns>
        /// <typeparam name="C">Integration command class</typeparam>
        /// <param name="subscriber">Integration command subscriber</param>
        public static Task SubscribeAsync<C>(this IIntegrationCommandSubscriber subscriber)
            where C : IntegrationCommand => subscriber.SubscribeAsync<C>(topic: null);

        /// <summary> Subscribes an integration command and registers its command handler. </summary>
        /// <returns>Task</returns>
        /// <typeparam name="C">Integration command class</typeparam>
        /// <param name="subscriber">Integration command subscriber</param>
        /// <param name="topic">Topic (optional) to further differentiate the integration command</param>
        public static Task SubscribeAsync<C>(this IIntegrationCommandSubscriber subscriber, string topic = null)
            where C : IntegrationCommand => subscriber.SubscribeAsync<C>(CancellationToken.None, topic);
    }
}
