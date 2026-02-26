
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;

namespace HotelBooking.Application.Common.Errors;
public static class ApplicationErrors
{
    public static class Auth
    {
        public static readonly Error EmailAlreadyRegistered =
            Error.Conflict("Auth.EmailAlreadyRegistered", "Email is already registered.");

        public static Error RegistrationFailed(string details) =>
            Error.Failure("Auth.RegistrationFailed", $"Registration failed: {details}");

        public static readonly Error InvalidCredentials =
            Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");

        public static readonly Error UserNotFound =
            Error.NotFound("Auth.UserNotFound", "User not found.");

        public static readonly Error UpdateFailed =
            Error.Failure("Auth.UpdateFailed", "Profile update failed.");

        public static readonly Error AccountLocked =
            Error.Failure("Auth.AccountLocked", "Account is temporarily locked. Please try again later");

        public static readonly Error InvalidRefreshToken =
            Error.Failure("Auth.InvalidRefreshToken", "Invalid Refresh Token");

        public static readonly Error RefreshTokenReuse =
            Error.Failure("Auth.RefreshTokenReuse", "Refresh Token Reuse");
    }
}