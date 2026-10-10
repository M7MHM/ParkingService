using MediatR;
using Parking.Application.Interfaces;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Parkings.Queries
{
    public record GetAvailableSpotsQuery(Guid ParkingLotId) : IRequest<IReadOnlyList<ParkingSpotResponse>>;
    public record ParkingSpotResponse(
        Guid Id,
        string SpotNumber,
        SpotType Type,
        SpotStatus Status,
        Money HourlyRate,
        Guid? CurrentBookingId,
        DateTime? ReservedUntil
    );
    public class GetAvailableSpotsQueryHandler : IRequestHandler<GetAvailableSpotsQuery, IReadOnlyList<ParkingSpotResponse>>
    {
        private readonly IParkingLotRepository _lotRepository;

        public GetAvailableSpotsQueryHandler(IParkingLotRepository lotRepository)
        {
            _lotRepository = lotRepository;
        }
        public async Task<IReadOnlyList<ParkingSpotResponse>> Handle(
            GetAvailableSpotsQuery request,
            CancellationToken cancellationToken)
        {
            var lot = await _lotRepository.GetByIdWithSpotsAsync(request.ParkingLotId, cancellationToken)
                ?? throw new InvalidOperationException("Parking lot not found.");

            return lot.Spots
                .Where(s => s.IsCurrentlyAvailable())
                .Select(s => new ParkingSpotResponse(
                    s.Id,
                    s.SpotNumber,
                    s.Type,
                    s.Status,
                    s.HourlyRate,
                    s.CurrentBookingId,
                    s.ReservedUntil
                ))
                .ToList();
        }
    }
}
