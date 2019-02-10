using Epos.Eventing;
using Epos.Eventing.RabbitMQ;

using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary> Extension methods for adding integration event/command publisher and subscriber. </summary>
    public static class EposEventingServiceCollectionExtensions
    {
        /// <summary> Adds the integration command publisher. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionFactory">Connection factory</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationCommandPublisherRabbitMQ(
            this IServiceCollection services, IConnectionFactory connectionFactory
        ) => services.AddSingleton<IIntegrationCommandPublisher>(
            sp => new RabbitMQIntegrationCommandPublisher(connectionFactory)
        );

        /// <summary> Adds the integration command subscriber. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionFactory">Connection factory</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationCommandSubscriberRabbitMQ(
            this IServiceCollection services, IConnectionFactory connectionFactory
        ) => services.AddSingleton<IIntegrationCommandSubscriber>(
            sp => new RabbitMQIntegrationCommandSubscriber(sp, connectionFactory)
        );

        /// <summary> Adds the integration event publisher. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionFactory">Connection factory</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationEventPublisherRabbitMQ(
            this IServiceCollection services, IConnectionFactory connectionFactory
        ) => services.AddSingleton<IIntegrationEventPublisher>(
            sp => new RabbitMQIntegrationEventPublisher(connectionFactory)
        );

        /// <summary> Adds the integration event subscriber. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionFactory">Connection factory</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationEventSubscriberRabbitMQ(
            this IServiceCollection services, IConnectionFactory connectionFactory
        ) => services.AddSingleton<IIntegrationEventSubscriber>(
            sp => new RabbitMQIntegrationEventSubscriber(sp, connectionFactory)
        );
    }
}
