using Parking.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Notification
{
    internal interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body, CancellationToken ct);
    }
    internal sealed class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        public SmtpEmailSender(SmtpSettings settings) => _settings = settings;
        public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port) { EnableSsl = _settings.UseSsl };

            if (!string.IsNullOrEmpty(_settings.User))
                client.Credentials = new NetworkCredential(_settings.User, _settings.Password);

            using var message = new MailMessage(new MailAddress(_settings.From), new MailAddress(to))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };

            await client.SendMailAsync(message, ct);
        }
    }
    internal static class EmailTemplates
    {
        public static (string Subject, string Body) Build(string routingKey, BookingEventMessage m) => routingKey switch
        {
            MessageRoutes.BookingConfirmed => (
                $"Parking booking confirmed - {OneLine(m.LotName)}",
                Compose(m, "Your parking booking is confirmed.")),

            MessageRoutes.BookingCancelled => (
                $"Parking booking cancelled - {OneLine(m.LotName)}",
                Compose(m, "Your parking booking was cancelled.")),

            MessageRoutes.BookingCompleted => (
                $"Parking receipt - {OneLine(m.LotName)}",
                Compose(m, "Thank you for parking with us. Your booking is complete.")),

            _ => throw new NotSupportedException($"No email template for routing key '{routingKey}'.")
        };
        private static string Compose(BookingEventMessage m, string headline) =>
            $"""
        Hello {OneLine(m.RecipientName)},

        {headline}

        Booking:  {m.BookingId}
        Lot:      {OneLine(m.LotName)}
        Spot:     {OneLine(m.SpotNumber)}
        From:     {Utc(m.StartTimeUtc)}
        Until:    {Utc(m.EndTimeUtc)}
        Total:    {m.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)} {m.Currency}

        (Times are shown in UTC.)
        """;
        private static string Utc(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new FormatException("Notification timestamps must be UTC.");

            return value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
        }
        private static string OneLine(string value) =>
            value.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }
}
