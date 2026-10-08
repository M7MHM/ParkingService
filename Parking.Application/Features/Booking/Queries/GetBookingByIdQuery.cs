using MediatR;
using Parking.Application.Interfaces;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BookingEntity = Parking.Domain.Entities.Booking;

namespace Parking.Application.Features.Booking.Queries
{
    public record GetBookingByIdQuery(Guid Id) : IRequest<BookingResponse?>;
    public record BookingResponse(
        Guid Id,
        Guid ParkingSpotId,
        Guid UserId,
        DateTime StartTime,
        DateTime EndTime,
        Money TotalPrice,
        BookingStatus Status,
        string? Notes,
        DateTime CreatedAt
    );
    public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingResponse?>
    {
        private readonly IRepository<BookingEntity> _bookingRepository;
        public GetBookingByIdQueryHandler(IRepository<BookingEntity> bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }
        public async Task<BookingResponse?> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var booking = await _bookingRepository.GetByIdAsync(request.Id, cancellationToken);

            if (booking is null) return null;

            return new BookingResponse(
                booking.Id,
                booking.ParkingSpotId,
                booking.UserId,
                booking.Period.StartTime,
                booking.Period.EndTime,
                booking.TotalPrice,
                booking.Status,
                booking.Notes,
                booking.CreatedAt
            );
        }
    }
}
