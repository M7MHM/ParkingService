using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Notification
{
    internal sealed record RabbitMqSettings(string Host, int Port, string User, string Password, string VirtualHost);
    internal sealed record SmtpSettings(string Host, int Port, string From, string? User, string? Password, bool UseSsl);
}
