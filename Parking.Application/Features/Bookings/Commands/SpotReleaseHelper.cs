using Parking.Application.Interfaces;
using Parking.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Bookings.Commands
{
    internal static class SpotReleaseHelper
    {
        public static async Task ReleaseAsync(
            IParkingSpotRepository spots,
            IParkingLotRepository lots,
            Guid spotId,
            CancellationToken cancellationToken)
        {
            var spot = await spots.GetByIdAsync(spotId, cancellationToken)
                ?? throw new EntityNotFoundException("ParkingSpot", spotId);

            var lot = await lots.GetByIdWithSpotsAsync(spot.ParkingLotId, cancellationToken)
                ?? throw new EntityNotFoundException("ParkingLot", spot.ParkingLotId);

            lot.ReleaseSpot(spotId);
        }
    }
}
