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
    public class ParkingLotRepository : BaseRepository<ParkingLot>, IParkingLotRepository
    {
        public ParkingLotRepository(AppDbContext context) : base(context) { }
        public async Task<ParkingLot?> GetByIdWithSpotsAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(p => p.Spots)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }
        public override void Delete(ParkingLot entity)
        {
            foreach (var spot in entity.Spots)
            {
                spot.MarkAsDeleted();
            }

            entity.MarkAsDeleted();
            _dbSet.Update(entity);
        }
    }
}
