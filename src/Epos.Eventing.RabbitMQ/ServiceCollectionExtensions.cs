using Microsoft.Extensions.DependencyInjection;

using RabbitMQ.Client;

namespace Epos.Eventing.RabbitMQ
{
    /// <summary> Extension methods for adding integration event publisher and subscriber. </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary> Adds the integration event publisher. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionFactory">Connection factory</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationEventPublisherRabbitMQ(
            this IServiceCollection services, IConnectionFactory connectionFactory
        ) {
            return services.AddSingleton<IIntegrationEventPublisher>(
                sp => new RabbitMQIntegrationEventPublisher(connectionFactory)
            );
        }

        /// <summary> Adds the integration event subscriber. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionFactory">Connection factory</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationEventSubscriberRabbitMQ(
            this IServiceCollection services, IConnectionFactory connectionFactory
        ) {
            return services.AddSingleton<IIntegrationEventSubscriber>(
                sp => new RabbitMQIntegrationEventSubscriber(sp, connectionFactory)
            );
        }
    }
}
