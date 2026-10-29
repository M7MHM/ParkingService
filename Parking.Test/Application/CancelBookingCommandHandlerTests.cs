using FluentAssertions;
using Moq;
using Parking.Application.Features.Bookings.Commands;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using Parking.Test.Common;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.Exceptions;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Application
{
    public sealed class CancelBookingCommandHandlerTests
    {
        private readonly Mock<IRepository<Booking>> _bookings = new(MockBehavior.Strict);
        private readonly Mock<IParkingSpotRepository> _spots = new(MockBehavior.Strict);
        private readonly Mock<IParkingLotRepository> _lots = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

        [Fact]
        public async Task Handle_OwnerCancelsConfirmedBooking_ReleasesSpotAndPersists()
        {
            var userId = Guid.NewGuid();
            var lot = TestData.LotWithReservedSpot(userId, out var booking, out var spot);
            SetupBooking(booking);
            SetupReleasePath(lot, spot);
            SetupSave();

            await CreateHandler().Handle(
                new CancelBookingCommand(booking.Id, userId, IsAdmin: false), CancellationToken.None);

            booking.Status.Should().Be(BookingStatus.Cancelled);
            spot.Status.Should().Be(SpotStatus.Available);
            spot.CurrentBookingId.Should().BeNull();
            lot.AvailableSpotsCount.Should().Be(1);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task Handle_AdminCancelsAnotherUsersBooking_Succeeds()
        {
            var lot = TestData.LotWithReservedSpot(Guid.NewGuid(), out var booking, out var spot);
            SetupBooking(booking);
            SetupReleasePath(lot, spot);
            SetupSave();

            await CreateHandler().Handle(
                new CancelBookingCommand(booking.Id, Guid.NewGuid(), IsAdmin: true), CancellationToken.None);

            booking.Status.Should().Be(BookingStatus.Cancelled);
            spot.Status.Should().Be(SpotStatus.Available);
        }
        [Fact]
        public async Task Handle_DifferentUserRequestsCancellation_ThrowsEntityNotFoundAndDoesNotRelease()
        {
            var lot = TestData.LotWithReservedSpot(Guid.NewGuid(), out var booking, out var spot);
            SetupBooking(booking);

            Func<Task> act = () => CreateHandler().Handle(
                new CancelBookingCommand(booking.Id, Guid.NewGuid(), IsAdmin: false), CancellationToken.None);

            await act.Should().ThrowAsync<EntityNotFoundException>()
                .Where(e => e.EntityName == "Booking" && e.EntityId == booking.Id);
            booking.Status.Should().Be(BookingStatus.Confirmed);
            spot.Status.Should().Be(SpotStatus.Reserved);
            _spots.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task Handle_BookingNotFound_ThrowsEntityNotFound()
        {
            var bookingId = Guid.NewGuid();
            _bookings
                .Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking?)null);

            Func<Task> act = () => CreateHandler().Handle(
                new CancelBookingCommand(bookingId, Guid.NewGuid(), IsAdmin: false), CancellationToken.None);

            await act.Should().ThrowAsync<EntityNotFoundException>();
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task Handle_OwnerCancelsPendingBooking_DoesNotReleaseSpot()
        {
            var ownerId = Guid.NewGuid();
            var lot = TestData.LotWithSpot(out var spot);
            var booking = TestData.PendingBooking(spot.Id, ownerId);
            SetupBooking(booking);
            SetupSave();

            await CreateHandler().Handle(
                new CancelBookingCommand(booking.Id, ownerId, IsAdmin: false), CancellationToken.None);

            booking.Status.Should().Be(BookingStatus.Cancelled);
            spot.Status.Should().Be(SpotStatus.Available);
            lot.AvailableSpotsCount.Should().Be(1);
            _spots.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _lots.Verify(r => r.GetByIdWithSpotsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task Handle_PeriodAlreadyStarted_ThrowsAndDoesNotReleaseOrSave()
        {
            var ownerId = Guid.NewGuid();
            var started = new BookingPeriod(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
            var booking = TestData.ConfirmedBooking(Guid.NewGuid(), ownerId, started);
            SetupBooking(booking);

            Func<Task> act = () => CreateHandler().Handle(
                new CancelBookingCommand(booking.Id, ownerId, IsAdmin: false), CancellationToken.None);

            await act.Should().ThrowAsync<BookingCannotBeCancelledException>();
            booking.Status.Should().Be(BookingStatus.Confirmed);
            _spots.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        private void SetupBooking(Booking booking) =>
            _bookings
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

        private void SetupReleasePath(ParkingLot lot, ParkingSpot spot)
        {
            _spots
                .Setup(r => r.GetByIdAsync(spot.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(spot);
            _lots
                .Setup(r => r.GetByIdWithSpotsAsync(lot.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lot);
        }

        private void SetupSave() =>
            _unitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

        private CancelBookingCommandHandler CreateHandler() =>
            new(_bookings.Object, _spots.Object, _lots.Object, _unitOfWork.Object);
    }
}
