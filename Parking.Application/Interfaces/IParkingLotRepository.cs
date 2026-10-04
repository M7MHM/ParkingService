using ParkingBooking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Interfaces
{
    public interface IParkingLotRepository :IRepository<ParkingLot>
    {
        Task<ParkingLot?> GetByIdWithSpotsAsync(Guid id, CancellationToken ct = default);
    }
}
