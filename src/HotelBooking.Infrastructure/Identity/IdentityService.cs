using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Common.Constants;
using HotelBooking.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Identity;

public class IdentityService(
    UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<bool> IsEmailUniqueAsync(
        string email, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        return existing is null;
    }

    public async Task<Result<UserAuthResult>> RegisterUserAsync(
    string email,
    string password,
    string firstName,
    string lastName,
    string? phoneNumber,
    CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email.Trim(),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var identityResult = await userManager.CreateAsync(user, password);

        if (!identityResult.Succeeded)
        {
            if (identityResult.Errors.Any(e =>
                string.Equals(e.Code, "DuplicateEmail", StringComparison.OrdinalIgnoreCase)))
            {
                return ApplicationErrors.Auth.EmailAlreadyRegistered;
            }

            if (identityResult.Errors.Any(e =>
                string.Equals(e.Code, "DuplicateUserName", StringComparison.OrdinalIgnoreCase)))
            {
                return ApplicationErrors.Auth.EmailAlreadyRegistered;
            }

            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            return ApplicationErrors.Auth.RegistrationFailed(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, HotelBookingConstants.Roles.User);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            return ApplicationErrors.Auth.RegistrationFailed(
                $"User created but role assignment failed: {roleErrors}");
        }

        return new UserAuthResult(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            [HotelBookingConstants.Roles.User],
            user.CreatedAtUtc);
    }

    public async Task<Result<UserAuthResult>> ValidateCredentialsAsync(
    string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return ApplicationErrors.Auth.InvalidCredentials;

        if (await userManager.IsLockedOutAsync(user))
            return ApplicationErrors.Auth.AccountLocked;

        var isValid = await userManager.CheckPasswordAsync(user, password);
        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            return ApplicationErrors.Auth.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);

        return new UserAuthResult(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            roles,
            user.CreatedAtUtc);
    }

    public async Task<Result<UserProfileResult>> GetUserByIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ApplicationErrors.Auth.UserNotFound;

        var roles = await userManager.GetRolesAsync(user);

        return new UserProfileResult(
            user.Id, user.Email!, user.FirstName, user.LastName,
            user.PhoneNumber, roles.FirstOrDefault() ?? HotelBookingConstants.Roles.User,
            user.CreatedAtUtc, user.UpdatedAtUtc);
    }

    public async Task<Result<UserProfileResult>> UpdateUserAsync(
        string userId, string firstName, string lastName,
        string? phoneNumber, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ApplicationErrors.Auth.UserNotFound;

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return ApplicationErrors.Auth.UpdateFailed;

        var roles = await userManager.GetRolesAsync(user);

        return new UserProfileResult(
            user.Id, user.Email!, user.FirstName, user.LastName,
            user.PhoneNumber, roles.FirstOrDefault() ?? HotelBookingConstants.Roles.User,
            user.CreatedAtUtc, user.UpdatedAtUtc);
    }
}