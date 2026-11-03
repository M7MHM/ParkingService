using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Parking.Contracts
{
    public static class MessageRoutes
    {
        public const string Exchange = "parking.events";

        public const string BookingConfirmed = "booking.confirmed";
        public const string BookingCancelled = "booking.cancelled";
        public const string BookingCompleted = "booking.completed";
    }

    public sealed record BookingEventMessage(
        Guid MessageId,
        DateTime OccurredAtUtc,
        Guid BookingId,
        Guid UserId,
        string RecipientEmail,
        string RecipientName,
        string LotName,
        string SpotNumber,
        DateTime StartTimeUtc,
        DateTime EndTimeUtc,
        decimal TotalAmount,
        string Currency);

    public static class MessageJson
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    }
}
