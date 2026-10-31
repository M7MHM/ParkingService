using MediatR;
using Parking.Application.Interfaces;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Parkings.Queries
{

    public sealed record GetParkingLotByIdQuery(Guid Id)
        : ICacheableQuery<ParkingLotResponse?>
    {
        public string CacheKey => $"parking-lot:{Id:D}";
        public TimeSpan CacheDuration => TimeSpan.FromSeconds(30);
        public string? CacheGroup => $"LotGroup:{Id:D}"; 
    }
    public record ParkingLotResponse(
        Guid Id,
        string Name,
        string Description,
        Location Location,
        Money HourlyRate,
        int TotalSpots,
        int AvailableSpotsCount,
        bool IsActive,
        TimeSpan OpeningTime,
        TimeSpan ClosingTime,
        DateTime CreatedAt
    );
    public class GetParkingLotByIdQueryHandler : IRequestHandler<GetParkingLotByIdQuery, ParkingLotResponse?>
    {
        private readonly IRepository<ParkingLot> _lotRepository;
        public GetParkingLotByIdQueryHandler(IRepository<ParkingLot> lotRepository)
        {
            _lotRepository = lotRepository;
        }
        public async Task<ParkingLotResponse?> Handle(GetParkingLotByIdQuery request, CancellationToken cancellationToken)
        {
            var lot = await _lotRepository.GetByIdAsync(request.Id, cancellationToken);

            if (lot is null) return null;

            return new ParkingLotResponse(
                lot.Id,
                lot.Name,
                lot.Description,
                lot.Location,
                lot.HourlyRate,
                lot.TotalSpots,
                lot.AvailableSpotsCount,
                lot.IsActive,
                lot.OpeningTime,
                lot.ClosingTime,
                lot.CreatedAt
            );
        }
    }
}
