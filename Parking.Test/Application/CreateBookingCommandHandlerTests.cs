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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Application
{
    public sealed class CreateBookingCommandHandlerTests
    {
        private readonly Mock<IParkingLotRepository> _lots = new(MockBehavior.Strict);
        private readonly Mock<IRepository<Booking>> _bookings = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

        [Fact]
        public async Task Handle_ValidRequest_CreatesConfirmsAndPersistsBooking()
        {
            var lot = TestData.LotWithSpot(out var spot);
            var command = CreateCommand(lot.Id, spot.Id);
            Booking? added = null;

            SetupLot(lot);
            _bookings
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .Callback<Booking, CancellationToken>((b, _) => added = b)
                .Returns(Task.CompletedTask);
            _unitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var bookingId = await CreateHandler().Handle(command, CancellationToken.None);

            bookingId.Should().NotBeEmpty();
            added.Should().NotBeNull();
            added!.Id.Should().Be(bookingId);
            added.UserId.Should().Be(command.UserId);
            added.Status.Should().Be(BookingStatus.Confirmed);
            added.Period.StartTime.Should().Be(command.StartTime);
            added.Period.EndTime.Should().Be(command.EndTime);
            added.TotalPrice.Amount.Should().Be(100m); 
            spot.Status.Should().Be(SpotStatus.Reserved);
            spot.CurrentBookingId.Should().Be(bookingId);
            lot.AvailableSpotsCount.Should().Be(0);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_LotIsClosed_ThrowsParkingLotClosedAndDoesNotPersist()
        {
            var lot = TestData.LotWithSpot(out var spot, openHour: 9, closeHour: 17);
            var command = CreateCommand(
                lot.Id, spot.Id, startTime: new DateTime(2030, 5, 10, 8, 0, 0, DateTimeKind.Utc));

            SetupLot(lot);

            Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ParkingLotClosedException>();
            _bookings.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task Handle_ParkingLotNotFound_ThrowsEntityNotFound()
        {
            var lotId = Guid.NewGuid();
            var command = CreateCommand(lotId, Guid.NewGuid());

            _lots
                .Setup(r => r.GetByIdWithSpotsAsync(lotId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ParkingLot?)null);

            Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<EntityNotFoundException>()
                .Where(e => e.EntityName == "ParkingLot" && e.EntityId == lotId);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task Handle_SpotDoesNotBelongToLot_ThrowsEntityNotFound()
        {
            var lot = TestData.Lot();
            var missingSpotId = Guid.NewGuid();
            var command = CreateCommand(lot.Id, missingSpotId);

            SetupLot(lot);

            Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<EntityNotFoundException>()
                .Where(e => e.EntityName == "ParkingSpot" && e.EntityId == missingSpotId);
            _bookings.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task Handle_SpotAlreadyReserved_ThrowsSpotNotAvailableAndDoesNotSave()
        {
            var lot = TestData.LotWithSpot(out var spot);
            lot.ReserveSpot(spot.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(2));
            var command = CreateCommand(lot.Id, spot.Id);

            SetupLot(lot);

            _bookings
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<SpotNotAvailableException>();
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            lot.AvailableSpotsCount.Should().Be(0);
        }
        private void SetupLot(ParkingLot lot) =>
            _lots
                .Setup(r => r.GetByIdWithSpotsAsync(lot.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lot);

        private CreateBookingCommandHandler CreateHandler() =>
            new(_lots.Object, _bookings.Object, _unitOfWork.Object);

        private static CreateBookingCommand CreateCommand(
            Guid lotId, Guid spotId, DateTime? startTime = null)
        {
            var start = startTime ?? DateTime.UtcNow.Date.AddDays(1).AddHours(10);
            return new CreateBookingCommand(
                lotId, spotId, Guid.NewGuid(), start, start.AddHours(1).AddMinutes(30), "Unit test booking");
        }
    }
}
