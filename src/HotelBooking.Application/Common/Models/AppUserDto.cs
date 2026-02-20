using System.Security.Claims;

namespace HotelBooking.Application.Common.Models
{
    public sealed record AppUserDto(string UserId, string Email, IList<string> Roles, IList<Claim> Claims);
}
