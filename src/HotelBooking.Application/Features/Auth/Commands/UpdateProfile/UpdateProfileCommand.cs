// src/HotelBooking.Application/Features/Auth/Commands/UpdateProfile/UpdateProfileCommand.cs
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    string UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IRequest<Result<ProfileResponse>>;
