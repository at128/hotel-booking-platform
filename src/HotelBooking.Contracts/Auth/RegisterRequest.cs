using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBooking.Contracts.Auth
{
    public sealed record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string? PhoneNumber);

}
