using FluentAssertions;
using Parking.Application.Features.Bookings.Commands;
using Parking.Application.Features.Parkings.Commands;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Application
{
    public sealed class ValidatorHandlerTest
    {
        public sealed class CreateParkingLotCommandValidatorTests
        {
            private readonly CreateParkingLotCommandValidator _validator = new();

            [Fact]
            public void ValidCommand_Passes()
            {
                _validator.Validate(Build()).IsValid.Should().BeTrue();
            }

            [Fact]
            public void TwentyFourHourLot_ZeroToTwentyFour_Passes()
            {
                _validator.Validate(Build(open: TimeSpan.Zero, close: TimeSpan.FromHours(24))).IsValid.Should().BeTrue();
            }

            [Theory]
            [InlineData(8, 8)]
            [InlineData(18, 8)]
            [InlineData(22, 6)]    
            [InlineData(0, 25)]
            [InlineData(24, 25)]
            public void InvalidOperatingHours_AreRejected(int openHour, int closeHour)
            {
                var result = _validator.Validate(
                    Build(open: TimeSpan.FromHours(openHour), close: TimeSpan.FromHours(closeHour)));

                result.IsValid.Should().BeFalse();
            }

            [Fact]
            public void MissingHoursBothDefaultToZero_IsRejected()
            {

                _validator.Validate(Build(open: TimeSpan.Zero, close: TimeSpan.Zero)).IsValid.Should().BeFalse();
            }

            [Fact]
            public void ZeroTotalSpots_IsRejected()
            {
                _validator.Validate(Build(totalSpots: 0)).IsValid.Should().BeFalse();
            }

            private static CreateParkingLotCommand Build(
                int totalSpots = 10,
                TimeSpan? open = null,
                TimeSpan? close = null) => new(
                    "Central Garage",
                    "Downtown",
                    new Location(30.0444, 31.2357),
                    new Money(50m),
                    totalSpots,
                    open ?? TimeSpan.FromHours(8),
                    close ?? TimeSpan.FromHours(18));
        }
        public sealed class CreateBookingCommandValidatorTests
        {
            private readonly CreateBookingCommandValidator _validator = new();

            [Fact]
            public void FutureBooking_Passes()
            {
                _validator.Validate(Build()).IsValid.Should().BeTrue();
            }

            [Fact]
            public void StartInThePast_IsRejected()
            {
                var start = DateTime.UtcNow.AddHours(-2);

                var result = _validator.Validate(Build(start: start, end: start.AddHours(1)));

                result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBookingCommand.StartTime));
            }

            [Theory]
            [InlineData(0)]
            [InlineData(-30)]
            public void EndNotAfterStart_IsRejected(int minutesAfterStart)
            {
                var start = DateTime.UtcNow.AddHours(1);

                var result = _validator.Validate(Build(start: start, end: start.AddMinutes(minutesAfterStart)));

                result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateBookingCommand.EndTime));
            }

            [Fact]
            public void EmptyIds_AreRejected()
            {
                var start = DateTime.UtcNow.AddHours(1);
                var command = new CreateBookingCommand(Guid.Empty, Guid.Empty, Guid.Empty, start, start.AddHours(1), null);

                var result = _validator.Validate(command);

                result.Errors.Select(e => e.PropertyName).Should().Contain(new[]
                {
                nameof(CreateBookingCommand.ParkingLotId),
                nameof(CreateBookingCommand.ParkingSpotId),
                nameof(CreateBookingCommand.UserId)
            });
            }

            [Fact]
            public void NotesLongerThan2000_IsRejected()
            {
                _validator.Validate(Build(notes: new string('n', 2001))).IsValid.Should().BeFalse();
            }

            private static CreateBookingCommand Build(DateTime? start = null, DateTime? end = null, string? notes = null)
            {
                var s = start ?? DateTime.UtcNow.AddHours(1);
                return new CreateBookingCommand(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), s, end ?? s.AddHours(1), notes);
            }
        }
    }
}
