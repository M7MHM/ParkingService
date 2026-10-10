using FluentValidation;
using MediatR;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parking.Application.Features.Bookings.Commands
{
    public record CreateBookingCommand(
           Guid ParkingLotId,
           Guid ParkingSpotId,
           Guid UserId,
           DateTime StartTime,
           DateTime EndTime,
           string? Notes
       ) : IRequest<Guid>;

    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
    {
        private readonly IParkingLotRepository _lotRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBookingCommandHandler(
            IParkingLotRepository lotRepository,
            IRepository<Booking> bookingRepository,
            IUnitOfWork unitOfWork)
        {
            _lotRepository = lotRepository;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var lot = await _lotRepository.GetByIdWithSpotsAsync(request.ParkingLotId, cancellationToken)
                    ??throw new InvalidOperationException("Parking lot not found.");
            if (!lot.IsOpenAt(request.StartTime))
                throw new InvalidOperationException("The parking lot is closed at the requested time.");

            var period = new BookingPeriod(request.StartTime, request.EndTime);

            var spot = lot.Spots.FirstOrDefault(s => s.Id == request.ParkingSpotId)
                ?? throw new InvalidOperationException("The parking spot does not exist in this lot.");

            var totalPrice = spot.CalculatePrice(period.WholeHoursCeiling());
            var booking = new Booking(
                request.ParkingSpotId,
                request.UserId,
                period,
                totalPrice,
                request.Notes
            );
            await _bookingRepository.AddAsync(booking, cancellationToken);

            lot.ReserveSpot(request.ParkingSpotId, booking.Id, request.EndTime);

            booking.Confirm();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return booking.Id;
        }
    }
    public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
    {
        public CreateBookingCommandValidator()
        {
            RuleFor(command => command.ParkingLotId).NotEmpty();
            RuleFor(command => command.ParkingSpotId).NotEmpty();
            RuleFor(command => command.UserId).NotEmpty();

            RuleFor(command => command.StartTime)
                .GreaterThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(-1))
                .WithMessage("Booking start must be now or in the future.");

            RuleFor(command => command.EndTime)
                .GreaterThan(command => command.StartTime)
                .WithMessage("Booking end must be after its start.");

            RuleFor(command => command.Notes)
                .MaximumLength(2000);
        }
    }
}
