using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Interfaces
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(object domainEvent, CancellationToken ct = default);
    }
}
