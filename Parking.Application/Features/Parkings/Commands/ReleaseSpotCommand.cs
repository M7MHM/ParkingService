using MediatR;
using Parking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Parkings.Commands
{
    public record ReleaseSpotCommand(
        Guid ParkingLotId,
        Guid SpotId
    ) : IRequest;

    public class ReleaseSpotCommandHandler : IRequestHandler<ReleaseSpotCommand>
    {
        private readonly IParkingLotRepository _lotRepository;
        private readonly IUnitOfWork _unitOfWork;
        public ReleaseSpotCommandHandler(
            IParkingLotRepository lotRepository,
            IUnitOfWork unitOfWork)
        {
            _lotRepository = lotRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(ReleaseSpotCommand request, CancellationToken cancellationToken)
        {
            var lot = await _lotRepository.GetByIdWithSpotsAsync(request.ParkingLotId, cancellationToken)
                ?? throw new InvalidOperationException("Parking lot not found.");

            lot.ReleaseSpot(request.SpotId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
