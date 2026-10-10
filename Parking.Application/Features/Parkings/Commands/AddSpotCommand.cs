using MediatR;
using Parking.Application.Interfaces;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Parkings.Commands
{
    public record AddSpotCommand(
        Guid ParkingLotId,
        string SpotNumber,
        SpotType Type,
        Money HourlyRate
    ) : IRequest<Guid>;
    public class AddSpotCommandHandler : IRequestHandler<AddSpotCommand, Guid>
    {
        private readonly IParkingLotRepository _lotRepository;
        private readonly IUnitOfWork _unitOfWork;
        public AddSpotCommandHandler(
            IParkingLotRepository lotRepository,
            IUnitOfWork unitOfWork)
        {
            _lotRepository = lotRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task<Guid> Handle(AddSpotCommand request, CancellationToken cancellationToken)
        {
            var lot = await _lotRepository.GetByIdWithSpotsAsync(request.ParkingLotId, cancellationToken)
                ?? throw new InvalidOperationException("Parking lot not found.");

            var spot = new ParkingSpot(
                request.SpotNumber,
                request.Type,
                request.HourlyRate,
                lot.Id
            );

            lot.AddSpot(spot);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return spot.Id;
        }
    }
}
