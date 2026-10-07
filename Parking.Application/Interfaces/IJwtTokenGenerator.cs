using Parking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
        TimeSpan TokenLifetime { get; }
    }
}
