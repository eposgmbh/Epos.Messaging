using System;
using System.Linq;

using Epos.Eventing;
using Epos.Eventing.RabbitMQ;

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

            return services.AddSingleton<IIntegrationCommandPublisher, RabbitMQIntegrationCommandPublisher>();
        }

        /// <summary> Adds the integration command subscriber. </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationCommandSubscriberRabbitMQ(this IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<IIntegrationCommandSubscriber, RabbitMQIntegrationCommandSubscriber>();
        }

        /// <summary> Adds an integration command handler. </summary>
        /// <typeparam name="T">Integration command handler type</typeparam>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationCommandHandler<T>(this IServiceCollection services) =>
            services.AddIntegrationCommandHandler(typeof(T));


        /// <summary> Adds an integration command handler. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="integrationCommandHandlerType">Integration command handler type</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationCommandHandler(
            this IServiceCollection services, Type integrationCommandHandlerType
        ) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            if (integrationCommandHandlerType == null) {
                throw new ArgumentNullException(nameof(integrationCommandHandlerType));
            }

            Type theInterfaceType = integrationCommandHandlerType.GetInterfaces().SingleOrDefault(
                i => i.GetGenericTypeDefinition() == typeof(IIntegrationCommandHandler<>)
            );

            if (theInterfaceType == null) {
                throw new ArgumentOutOfRangeException(
                    nameof(integrationCommandHandlerType),
                    "The handler must implement the IIntegrationCommandHandler interface."
                );
            }

            return services.AddScoped(theInterfaceType, integrationCommandHandlerType);
        }
    }
}
