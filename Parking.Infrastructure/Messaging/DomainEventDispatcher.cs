using Microsoft.Extensions.DependencyInjection;
using Parking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Messaging
{
    public sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _services;
        public DomainEventDispatcher(IServiceProvider services) => _services = services;

        public async Task DispatchAsync(object domainEvent, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<object>.HandleAsync))
                ?? throw new InvalidOperationException($"Handler method was not found for {domainEvent.GetType().Name}.");

            foreach (var handler in _services.GetServices(handlerType))
            {
                var task = (Task?)method.Invoke(handler, new[] { domainEvent, ct });
                if (task is null)
                    throw new InvalidOperationException($"Handler {handlerType.Name} returned a null Task.");

                await task;
            }
        }
    }

    public sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(object domainEvent, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
