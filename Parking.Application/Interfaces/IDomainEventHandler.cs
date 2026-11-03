using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Interfaces
{
    public interface IDomainEventHandler<in TEvent>
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken ct);
    }
}
