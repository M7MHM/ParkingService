using MediatR;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using ParkingBooking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Bookings.Commands
{
    public sealed record CancelBookingCommand(Guid BookingId, Guid RequestingUserId, bool IsAdmin) : IRequest;
    public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IParkingSpotRepository _spots;
        private readonly IParkingLotRepository _lots;
        private readonly IUnitOfWork _unitOfWork;
        public CancelBookingCommandHandler(
            IRepository<Booking> bookings,
            IParkingSpotRepository spots,
            IParkingLotRepository lots,
            IUnitOfWork unitOfWork)
        {
            _bookings = bookings;
            _spots = spots;
            _lots = lots;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken)
                ?? throw new EntityNotFoundException("Booking", request.BookingId);

            if (!request.IsAdmin && booking.UserId != request.RequestingUserId)
                throw new EntityNotFoundException("Booking", request.BookingId);

            var wasConfirmed = booking.Status == BookingStatus.Confirmed;
            booking.Cancel();

            if (wasConfirmed)
                await SpotReleaseHelper.ReleaseAsync(_spots, _lots, booking.ParkingSpotId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
