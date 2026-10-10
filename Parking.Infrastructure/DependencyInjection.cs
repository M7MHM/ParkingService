using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parking.Application.Interfaces;
using Parking.Infrastructure.Persistence.Data;
using Parking.Infrastructure.Persistence.Repositories;
using Parking.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=ParkingBooking.db";

            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IParkingLotRepository, ParkingLotRepository>();
            services.AddScoped<IParkingSpotRepository, ParkingSpotRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddTransient<IEmailService, FakeEmailService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }
    }
}
