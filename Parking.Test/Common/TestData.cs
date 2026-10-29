using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Domain.ValueObjects;
using ParkingBooking.Domain.Entities;
using ParkingBooking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Common
{
    internal static class TestData
    {
        public static ParkingLot Lot(double openHour = 0, double closeHour = 24, int totalSpots = 10) => new(
            "Central Garage",
            "Downtown parking",
            new Location(30.0444, 31.2357, "Cairo"),
            new Money(50m),
            totalSpots,
            TimeSpan.FromHours(openHour),
            TimeSpan.FromHours(closeHour));
        public static ParkingSpot Spot(ParkingLot lot, string number = "A-01") =>
            new(number, SpotType.Standard(), new Money(50m), lot.Id);

        public static ParkingLot LotWithSpot(out ParkingSpot spot, double openHour = 0, double closeHour = 24)
        {
            var lot = Lot(openHour, closeHour);
            spot = Spot(lot);
            lot.AddSpot(spot);
            return lot;
        }
        public static BookingPeriod FuturePeriod(int startInHours = 2, int durationHours = 1)
        {
            var start = DateTime.UtcNow.AddHours(startInHours);
            return new BookingPeriod(start, start.AddHours(durationHours));
        }
        public static Booking PendingBooking(Guid spotId, Guid userId, BookingPeriod? period = null) =>
            new(spotId, userId, period ?? FuturePeriod(), new Money(50m));

        public static Booking ConfirmedBooking(Guid spotId, Guid userId, BookingPeriod? period = null)
        {
            var booking = PendingBooking(spotId, userId, period);
            booking.Confirm();
            return booking;
        }
        public static ParkingLot LotWithReservedSpot(
            Guid userId, out Booking booking, out ParkingSpot spot, BookingPeriod? period = null)
        {
            var lot = LotWithSpot(out spot);
            booking = ConfirmedBooking(spot.Id, userId, period);
            lot.ReserveSpot(spot.Id, booking.Id, booking.Period.EndTime);
            return lot;
        }
        public static User CustomerUser(string email = "user@example.com") =>
            new(new Email(email), "stored-hash", "Test User", UserRole.Customer);
    }
}
