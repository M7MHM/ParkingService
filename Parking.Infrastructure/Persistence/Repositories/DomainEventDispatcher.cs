using Parking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Repositories
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(object domainEvent, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
