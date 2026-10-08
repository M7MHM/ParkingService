using MediatR;
using Parking.Application.Interfaces;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Parking.Commands
{
    public record CreateParkingLotCommand(
        string Name,
        string Description,
        Location Location,
        Money HourlyRate,
        int TotalSpots,
        TimeSpan OpeningTime,
        TimeSpan ClosingTime
    ) : IRequest<Guid>;

    public class CreateParkingLotCommandHandler : IRequestHandler<CreateParkingLotCommand, Guid>
    {
        private readonly IRepository<ParkingLot> _lotRepository;
        private readonly IUnitOfWork _unitOfWork;
        public CreateParkingLotCommandHandler(
            IRepository<ParkingLot> lotRepository,
            IUnitOfWork unitOfWork)
        {
            _lotRepository = lotRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateParkingLotCommand request, CancellationToken cancellationToken)
        {
            var lot = new ParkingLot(
                request.Name,
                request.Description,
                request.Location,
                request.HourlyRate,
                request.TotalSpots,
                request.OpeningTime,
                request.ClosingTime
            );
            await _lotRepository.AddAsync(lot, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return lot.Id;
        }
    }
}

