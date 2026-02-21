// src/HotelBooking.Application/Features/Auth/Commands/Register/RegisterCommand.cs
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IRequest<Result<AuthResponse>>;
