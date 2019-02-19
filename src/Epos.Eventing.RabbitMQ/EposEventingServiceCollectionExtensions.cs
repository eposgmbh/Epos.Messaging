using System;
using System.Linq;

using Epos.Eventing;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary> Extension methods for adding integration event/command publisher and subscriber. </summary>
    public static class EposEventingServiceCollectionExtensions
    {
        /// <summary> Adds the integration command publisher. </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationCommandPublisherRabbitMQ(this IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<IIntegrationCommandPublisher>();
        }

        /// <summary> Adds the integration command subscriber. </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationCommandSubscriberRabbitMQ(this IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<IIntegrationCommandSubscriber>();
        }

        /// <summary> Adds an integration command handler. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="integrationCommandHandlerType">Integration command handler type</param>
        /// <returns></returns>
        public static IServiceCollection AddIntegrationCommandHandler(
            this IServiceCollection services, Type integrationCommandHandlerType
        ) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            if (integrationCommandHandlerType == null) {
                throw new ArgumentNullException(nameof(integrationCommandHandlerType));
            }

            Type theInterfaceType = integrationCommandHandlerType.GetInterfaces().Single(
                i => i.GetGenericTypeDefinition() == typeof(IIntegrationCommandHandler<>)
            );

            return services.AddScoped(theInterfaceType, integrationCommandHandlerType);
        }
    }
}
