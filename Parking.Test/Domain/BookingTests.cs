using FluentAssertions;
using Parking.Test.Common;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.Events;
using ParkingBooking.Domain.Exceptions;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Domain
{
    public sealed class BookingTests
    {
        private static readonly Guid SpotId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();

        [Fact]
        public void Constructor_StartsPendingAndRaisesCreatedEvent()
        {
            var booking = TestData.PendingBooking(SpotId, UserId);

            booking.Status.Should().Be(BookingStatus.Pending);
            booking.DomainEvents.OfType<BookingCreatedEvent>().Should().ContainSingle()
                .Which.Should().Match<BookingCreatedEvent>(e =>
                    e.BookingId == booking.Id && e.ParkingSpotId == SpotId && e.UserId == UserId);
        }
        [Fact]
        public void Confirm_PendingBooking_BecomesConfirmedAndRaisesEvent()
        {
            var booking = TestData.PendingBooking(SpotId, UserId);

            booking.Confirm();

            booking.Status.Should().Be(BookingStatus.Confirmed);
            booking.DomainEvents.OfType<BookingConfirmedEvent>().Should().ContainSingle();
        }
        [Fact]
        public void Confirm_CalledTwice_ThrowsInvalidTransition()
        {
            var booking = TestData.ConfirmedBooking(SpotId, UserId);

            Action act = () => booking.Confirm();

            act.Should().Throw<InvalidBookingStatusTransitionException>()
                .Where(e => e.CurrentStatus == BookingStatus.Confirmed
                         && e.AttemptedStatus == BookingStatus.Confirmed);
        }
        [Fact]
        public void Cancel_ConfirmedBookingBeforeStart_CancelsAndRaisesCancelledEvent()
        {
            var booking = TestData.ConfirmedBooking(SpotId, UserId);

            booking.Cancel();

            booking.Status.Should().Be(BookingStatus.Cancelled);
            booking.DomainEvents.OfType<BookingCancelledEvent>().Should().ContainSingle();
        }
        [Fact]
        public void Cancel_PendingBooking_CancelsWithoutCancelledEvent()
        {
            var booking = TestData.PendingBooking(SpotId, UserId);

            booking.Cancel();

            booking.Status.Should().Be(BookingStatus.Cancelled);
            booking.DomainEvents.OfType<BookingCancelledEvent>().Should().BeEmpty();
        }
        [Fact]
        public void Cancel_AfterPeriodStarted_ThrowsAndKeepsStatus()
        {
            var startedPeriod = new BookingPeriod(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
            var booking = TestData.ConfirmedBooking(SpotId, UserId, startedPeriod);

            Action act = () => booking.Cancel();

            act.Should().Throw<BookingCannotBeCancelledException>();
            booking.Status.Should().Be(BookingStatus.Confirmed);
        }
        [Fact]
        public void Cancel_AlreadyCancelled_ThrowsInvalidTransition()
        {
            var booking = TestData.PendingBooking(SpotId, UserId);
            booking.Cancel();

            Action act = () => booking.Cancel();

            act.Should().Throw<InvalidBookingStatusTransitionException>();
        }
        [Fact]
        public void Complete_ConfirmedBooking_CompletesAndRaisesEvent()
        {
            var booking = TestData.ConfirmedBooking(SpotId, UserId);

            booking.Complete();

            booking.Status.Should().Be(BookingStatus.Completed);
            booking.DomainEvents.OfType<BookingCompletedEvent>().Should().ContainSingle();
        }
        [Fact]
        public void Complete_PendingBooking_ThrowsInvalidTransition()
        {
            var booking = TestData.PendingBooking(SpotId, UserId);

            Action act = () => booking.Complete();

            act.Should().Throw<InvalidBookingStatusTransitionException>();
        }
        [Fact]
        public void Cancel_CompletedBooking_ThrowsInvalidTransition()
        {
            var booking = TestData.ConfirmedBooking(SpotId, UserId);
            booking.Complete();

            Action act = () => booking.Cancel();

            act.Should().Throw<InvalidBookingStatusTransitionException>();
        }
        [Fact]
        public void MarkExpired_PendingBooking_BecomesExpired()
        {
            var booking = TestData.PendingBooking(SpotId, UserId);

            booking.MarkExpired();

            booking.Status.Should().Be(BookingStatus.Expired);
        }
        [Fact]
        public void MarkExpired_ConfirmedBooking_ThrowsInvalidTransition()
        {
            var booking = TestData.ConfirmedBooking(SpotId, UserId);

            Action act = () => booking.MarkExpired();

            act.Should().Throw<InvalidBookingStatusTransitionException>();
        }
    }
}
