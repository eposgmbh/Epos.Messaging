using Microsoft.Extensions.DependencyInjection;

using RabbitMQ.Client;

namespace Epos.Eventing.RabbitMQ
{
    /// <summary> Extension methods for adding integration event publisher and registry. </summary>
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

        /// <summary> Adds the integration event registry. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionFactory">Connection factory</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationEventRegistryRabbitMQ(
            this IServiceCollection services, IConnectionFactory connectionFactory
        ) {
            return services.AddSingleton<IIntegrationEventRegistry>(
                sp => new RabbitMQIntegrationEventRegistry(sp, connectionFactory)
            );
        }
    }
}
