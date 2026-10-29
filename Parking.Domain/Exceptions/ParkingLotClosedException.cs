using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Domain.Exceptions
{
    public sealed class ParkingLotClosedException : Exception
    {
        public Guid ParkingLotId { get; }
        public DateTime RequestedTime { get; }
        public ParkingLotClosedException(Guid parkingLotId, DateTime requestedTime)
            : base($"The parking lot {parkingLotId} is closed at the requested time ({requestedTime:u}).")
        {
            ParkingLotId = parkingLotId;
            RequestedTime = requestedTime;
        }
    }
}
