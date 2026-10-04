using Microsoft.Extensions.Logging;
using Parking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Repositories
{
    public class FakeEmailService : IEmailService
    {
        private readonly ILogger<FakeEmailService> _logger;

        public FakeEmailService(ILogger<FakeEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            _logger.LogInformation("Fake email → To: {To} | Subject: {Subject}", to, subject);

            return Task.CompletedTask;
        }
    }
}
