using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var db = provider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(ct);

            var config = provider.GetRequiredService<IConfiguration>();
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            if (password.Length < 15)
                throw new InvalidOperationException("Admin password must be at least 15 characters long.");

            var users = provider.GetRequiredService<IUserRepository>();
            if (await users.GetByEmailAsync(email, ct) is not null)
                return;

            var hasher = provider.GetRequiredService<IPasswordHasher>();
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

            var admin = new User(
                new Email(email),
                hasher.Hash(password),
                config["Seed:AdminName"] ?? "Administrator",
                UserRole.Admin);

            await users.AddAsync(admin, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
