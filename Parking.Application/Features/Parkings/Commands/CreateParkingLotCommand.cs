using FluentValidation;
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
    public sealed class CreateParkingLotCommandValidator : AbstractValidator<CreateParkingLotCommand>
    {
        public CreateParkingLotCommandValidator()
        {
            RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
            RuleFor(command => command.Description).MaximumLength(1000);
            RuleFor(command => command.TotalSpots).GreaterThan(0);

            RuleFor(command => command.OpeningTime)
                .GreaterThanOrEqualTo(TimeSpan.Zero)
                .LessThan(TimeSpan.FromHours(24));

            RuleFor(command => command.ClosingTime)
                .GreaterThan(command => command.OpeningTime)
                .WithMessage("Closing time must be later than opening time.")
                .LessThanOrEqualTo(TimeSpan.FromHours(24));
        }
    }
}

