using Microsoft.EntityFrameworkCore;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Infrastructure.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Infrastructure.Persistence.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }
        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();

            return await _dbSet.FirstOrDefaultAsync(u => u.Email.Value == normalized, ct);
        }
    }
}
