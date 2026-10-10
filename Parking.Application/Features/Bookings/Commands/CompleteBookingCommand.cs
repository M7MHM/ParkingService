using MediatR;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Bookings.Commands
{
    public sealed record CompleteBookingCommand(Guid BookingId) : IRequest;
    public sealed class CompleteBookingCommandHandler : IRequestHandler<CompleteBookingCommand>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IParkingSpotRepository _spots;
        private readonly IParkingLotRepository _lots;
        private readonly IUnitOfWork _unitOfWork;
        public CompleteBookingCommandHandler(
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
        public async Task Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken)
                ?? throw new EntityNotFoundException("Booking", request.BookingId);

            booking.Complete();
            await SpotReleaseHelper.ReleaseAsync(_spots, _lots, booking.ParkingSpotId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
