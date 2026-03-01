using System.Security.Claims;
using HotelBooking.Application.Common.Interfaces;

namespace HotelBooking.Api.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
    public string? Id => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}