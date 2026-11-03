using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parking.Application.Interfaces;
using ParkingBooking.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Messaging
{
    internal sealed record RabbitMqSettings(
        string Host,
        int Port,
        string User,
        string Password,
        string VirtualHost);

    public static class MessagingServiceRegistration
    {
        public static IServiceCollection AddMessaging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            var host = configuration["RabbitMq:Host"];
            if (string.IsNullOrWhiteSpace(host))
                return services;

            var user = configuration["RabbitMq:User"];
            var password = configuration["RabbitMq:Password"];
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "RabbitMq:User and RabbitMq:Password are required when RabbitMq:Host is set.");

            var port = int.TryParse(configuration["RabbitMq:Port"], out var parsedPort)
                ? parsedPort
                : 5672;
            if (port is < 1 or > 65535)
                throw new InvalidOperationException("RabbitMq:Port must be between 1 and 65535.");

            services.AddSingleton(new RabbitMqSettings(
                host,
                port,
                user,
                password,
                configuration["RabbitMq:VirtualHost"] ?? "/"));

            services.AddSingleton<RabbitMqPublisher>();
            services.AddScoped<BookingOutboxWriter>();
            services.AddScoped<IDomainEventHandler<BookingConfirmedEvent>, BookingConfirmedOutboxHandler>();
            services.AddScoped<IDomainEventHandler<BookingCancelledEvent>, BookingCancelledOutboxHandler>();
            services.AddScoped<IDomainEventHandler<BookingCompletedEvent>, BookingCompletedOutboxHandler>();
            services.AddHostedService<OutboxPublisherService>();

            return services;
        }
    }
}
