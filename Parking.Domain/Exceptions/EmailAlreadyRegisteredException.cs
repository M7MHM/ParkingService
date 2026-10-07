using ParkingBooking.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Domain.Exceptions
{
    public class EmailAlreadyRegisteredException : DomainException
    {
        public EmailAlreadyRegisteredException(string email)
            : base($"The email {email} is already registered.")
        {
        }
    }
}
