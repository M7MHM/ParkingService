using ParkingBooking.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Domain.Exceptions
{
    public sealed class EntityNotFoundException : DomainException
    {
        public string EntityName { get; }
        public Guid EntityId { get; }

        public EntityNotFoundException(string entityName, Guid entityId)
            : base($"{entityName} with id '{entityId}' was not found.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
