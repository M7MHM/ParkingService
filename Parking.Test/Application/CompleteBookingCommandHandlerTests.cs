using FluentAssertions;
using Moq;
using Parking.Application.Features.Bookings.Commands;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using Parking.Test.Common;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Application
{
    public sealed class CompleteBookingCommandHandlerTests
    {
        private readonly Mock<IRepository<Booking>> _bookings = new(MockBehavior.Strict);
        private readonly Mock<IParkingSpotRepository> _spots = new(MockBehavior.Strict);
        private readonly Mock<IParkingLotRepository> _lots = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

        [Fact]
        public async Task Handle_ConfirmedBooking_CompletesReleasesSpotAndPersists()
        {
            var lot = TestData.LotWithReservedSpot(Guid.NewGuid(), out var booking, out var spot);
            _bookings.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
            _spots.Setup(r => r.GetByIdAsync(spot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(spot);
            _lots.Setup(r => r.GetByIdWithSpotsAsync(lot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lot);
            _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await CreateHandler().Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None);

            booking.Status.Should().Be(BookingStatus.Completed);
            spot.Status.Should().Be(SpotStatus.Available);
            lot.AvailableSpotsCount.Should().Be(1);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_PendingBooking_ThrowsInvalidTransitionAndDoesNotSave()
        {
            var booking = TestData.PendingBooking(Guid.NewGuid(), Guid.NewGuid());
            _bookings.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

            Func<Task> act = () => CreateHandler().Handle(new CompleteBookingCommand(booking.Id), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidBookingStatusTransitionException>();
            _spots.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_BookingNotFound_ThrowsEntityNotFound()
        {
            var id = Guid.NewGuid();
            _bookings.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

            Func<Task> act = () => CreateHandler().Handle(new CompleteBookingCommand(id), CancellationToken.None);

            await act.Should().ThrowAsync<EntityNotFoundException>();
        }

        private CompleteBookingCommandHandler CreateHandler() =>
            new(_bookings.Object, _spots.Object, _lots.Object, _unitOfWork.Object);
    }
}
