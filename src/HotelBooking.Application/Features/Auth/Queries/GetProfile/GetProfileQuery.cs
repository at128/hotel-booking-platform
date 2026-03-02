// src/HotelBooking.Application/Features/Auth/Queries/GetProfile/GetProfileQuery.cs
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;
using MediatR;

namespace HotelBooking.Application.Features.Auth.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<Result<ProfileResponse>>;
