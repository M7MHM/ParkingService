using FluentAssertions;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Domain
{
    public sealed class BookingPeriodTests
    {
        private static readonly DateTime Start = new(2030, 5, 10, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Constructor_EndTimeEqualsStartTime_ThrowsArgumentException()
        {
            Action act = () => new BookingPeriod(Start, Start);

            act.Should().Throw<ArgumentException>().WithParameterName("endTime");
        }
        [Fact]
        public void Constructor_EndTimeBeforeStartTime_ThrowsArgumentException()
        {
            Action act = () => new BookingPeriod(Start, Start.AddMinutes(-1));

            act.Should().Throw<ArgumentException>().WithParameterName("endTime");
        }
        [Fact]
        public void Constructor_EndTimeAfterStartTime_CreatesPeriodWithExpectedDuration()
        {
            var end = Start.AddHours(1).AddMinutes(30);

            var period = new BookingPeriod(Start, end);

            period.StartTime.Should().Be(Start);
            period.EndTime.Should().Be(end);
            period.Duration.Should().Be(TimeSpan.FromMinutes(90));
        }
        [Theory]
        [InlineData(1, 1)]
        [InlineData(60, 1)]
        [InlineData(61, 2)]
        [InlineData(120, 2)]
        public void WholeHoursCeiling_DurationRoundsUpToWholeHours(int minutes, int expectedHours)
        {
            var period = new BookingPeriod(Start, Start.AddMinutes(minutes));

            period.WholeHoursCeiling().Should().Be(expectedHours);
        }
        [Fact]
        public void Overlaps_IntersectingPeriods_ReturnsTrue()
        {
            var first = new BookingPeriod(Start, Start.AddHours(2));
            var second = new BookingPeriod(Start.AddHours(1), Start.AddHours(3));

            first.Overlaps(second).Should().BeTrue();
            second.Overlaps(first).Should().BeTrue();
        }
        [Fact]
        public void Overlaps_AdjacentPeriods_ReturnsFalse()
        {
            var first = new BookingPeriod(Start, Start.AddHours(1));
            var second = new BookingPeriod(Start.AddHours(1), Start.AddHours(2));

            first.Overlaps(second).Should().BeFalse();
        }
        [Fact]
        public void HasStarted_AsOfStartTime_ReturnsTrue()
        {
            var period = new BookingPeriod(Start, Start.AddHours(1));

            period.HasStarted(Start).Should().BeTrue();
            period.HasStarted(Start.AddTicks(-1)).Should().BeFalse();
        }
        [Fact]
        public void Equality_SameStartAndEnd_AreEqual()
        {
            var a = new BookingPeriod(Start, Start.AddHours(1));
            var b = new BookingPeriod(Start, Start.AddHours(1));

            a.Should().Be(b);
            (a == b).Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }
    }
}
