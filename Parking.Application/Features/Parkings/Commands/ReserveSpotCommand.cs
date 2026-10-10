using MediatR;
using Parking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Parkings.Commands
{
    public record ReserveSpotCommand(
        Guid ParkingLotId,
        Guid SpotId,
        Guid BookingId,
        DateTime ReservedUntil
    ) : IRequest;
    public class ReserveSpotCommandHandler : IRequestHandler<ReserveSpotCommand>
    {
        private readonly IParkingLotRepository _lotRepository;
        private readonly IUnitOfWork _unitOfWork;
        public ReserveSpotCommandHandler(
            IParkingLotRepository lotRepository,
            IUnitOfWork unitOfWork)
        {
            _lotRepository = lotRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(ReserveSpotCommand request, CancellationToken cancellationToken)
        {
            var lot = await _lotRepository.GetByIdWithSpotsAsync(request.ParkingLotId, cancellationToken)
                ?? throw new InvalidOperationException("Parking lot not found.");

            lot.ReserveSpot(request.SpotId, request.BookingId, request.ReservedUntil);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
