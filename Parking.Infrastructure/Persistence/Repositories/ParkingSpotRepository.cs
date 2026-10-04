using Microsoft.EntityFrameworkCore;
using Parking.Application.Interfaces;
using Parking.Infrastructure.Persistence.Data;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Repositories
{
    public class ParkingSpotRepository : BaseRepository<ParkingSpot>, IParkingSpotRepository
    {
        public ParkingSpotRepository(AppDbContext context) : base(context) { }
        public async Task<IReadOnlyList<ParkingSpot>> GetAvailableSpotsAsync(
            Guid parkingLotId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(s => s.ParkingLotId == parkingLotId && s.Status == SpotStatus.Available)
                .ToListAsync(ct);
        }
    }
}
