using System;
using System.Linq;

using Epos.Messaging;
using Epos.Messaging.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary> Extension methods for adding integration event/commanrequest publisher and subscriber. </summary>
    public static class EposMessagingServiceCollectionExtensions
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

        /// <summary> Adds the request/reply messenger. </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationRequestPublisherRabbitMQ(this IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<IIntegrationRequestPublisher, RabbitMQIntegrationRequestPublisher>();
        }

        /// <summary> Adds the request subscriber. </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationRequestSubscriberRabbitMQ(this IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<IIntegrationRequestSubscriber, RabbitMQIntegrationRequestSubscriber>();
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

        /// <summary> Adds an integration request handler. </summary>
        /// <typeparam name="T">Integration request handler type</typeparam>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationRequestHandler<T>(this IServiceCollection services) =>
            services.AddIntegrationRequestHandler(typeof(T));


        /// <summary> Adds an integration request handler. </summary>
        /// <param name="services">Service collection</param>
        /// <param name="integrationRequestHandlerType">Integration request handler type</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddIntegrationRequestHandler(
            this IServiceCollection services, Type integrationRequestHandlerType
        ) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            if (integrationRequestHandlerType == null) {
                throw new ArgumentNullException(nameof(integrationRequestHandlerType));
            }

            Type theInterfaceType = integrationRequestHandlerType.GetInterfaces().SingleOrDefault(
                i => i.GetGenericTypeDefinition() == typeof(IIntegrationRequestHandler<,>)
            );

            if (theInterfaceType == null) {
                throw new ArgumentOutOfRangeException(
                    nameof(integrationRequestHandlerType),
                    "The handler must implement the IIntegrationRequestHandler interface."
                );
            }

            return services.AddScoped(theInterfaceType, integrationRequestHandlerType);
        }
    }
}
