using FluentValidation;
using MediatR;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using ParkingBooking.Domain.Exceptions;
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
       ) : ICacheInvalidatingRequest, IRequest<Guid>;

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
            var startTimeUtc = NormalizeUtc(request.StartTime);
            var endTimeUtc = NormalizeUtc(request.EndTime);

            var lot = await _lotRepository.GetByIdWithSpotsAsync(request.ParkingLotId, cancellationToken)
                ?? throw new EntityNotFoundException("ParkingLot", request.ParkingLotId);

            if (!lot.IsOpenAt(startTimeUtc))
                throw new ParkingLotClosedException(lot.Id, startTimeUtc);

            var period = new BookingPeriod(startTimeUtc, endTimeUtc);

            var spot = lot.Spots.FirstOrDefault(s => s.Id == request.ParkingSpotId)
                ?? throw new EntityNotFoundException("ParkingSpot", request.ParkingSpotId);

            var totalPrice = spot.CalculatePrice(period.WholeHoursCeiling());
            var booking = new Booking(
                request.ParkingSpotId,
                request.UserId,
                period,
                totalPrice,
                request.Notes
            );
            await _bookingRepository.AddAsync(booking, cancellationToken);

            lot.ReserveSpot(request.ParkingSpotId, booking.Id, endTimeUtc);

            booking.Confirm();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return booking.Id;
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => throw new ValidationException(
                "Booking timestamps must include a timezone, such as Z or +02:00."),
            _ => throw new ValidationException("Unsupported booking timestamp.")
        };
    }
    public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
    {
        public CreateBookingCommandValidator()
        {
            RuleFor(command => command.ParkingLotId).NotEmpty();
            RuleFor(command => command.ParkingSpotId).NotEmpty();
            RuleFor(command => command.UserId).NotEmpty();

            RuleFor(command => command.StartTime)
                            .Must(value =>
                                value.Kind != DateTimeKind.Unspecified &&
                                value.ToUniversalTime() >= DateTime.UtcNow.AddMinutes(-1))
                            .WithMessage("Booking start must include a timezone and be now or in the future.");

            RuleFor(command => command.EndTime)
                .Must((command, endTime) =>
                    endTime.Kind != DateTimeKind.Unspecified &&
                    command.StartTime.Kind != DateTimeKind.Unspecified &&
                    endTime.ToUniversalTime() > command.StartTime.ToUniversalTime())
                .WithMessage("Booking end must include a timezone and be after its start.");

            RuleFor(command => command.Notes)
                .MaximumLength(2000);
        }
    }
}
