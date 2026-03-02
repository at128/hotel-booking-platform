using HotelBooking.Domain.Common.Results;

namespace HotelBooking.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default);

    Task<Result<UserAuthResult>> RegisterUserAsync(
        string email, string password, string firstName, string lastName,
        string? phoneNumber, CancellationToken ct = default);

    Task<Result<UserAuthResult>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default);

    Task<Result<UserProfileResult>> GetUserByIdAsync(
        Guid userId, CancellationToken ct = default);

    Task<Result<UserProfileResult>> UpdateUserAsync(
        string userId, string firstName, string lastName,
        string? phoneNumber, CancellationToken ct = default);
}


public sealed record UserAuthResult(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IList<string> Roles,
    DateTimeOffset CreatedAt);  

public sealed record UserProfileResult(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Role,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);