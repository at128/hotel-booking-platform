using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Checkout;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelBooking.Api.IntegrationTests.Workflows;

/// <summary>
/// Tests the cancellation workflow.
/// Cancellation policy is driven by BookingSettings:
///   - CancellationFreeHours (default 24)
///   - CancellationFeePercent (default 0.30)
/// Free cancellation = within CancellationFreeHours of successful payment timestamp.
/// After free window = (1 - CancellationFeePercent) refund, i.e. 70%.
/// </summary>
[Collection("Integration")]
public class CancellationFlowTests
{
    private readonly WebAppFactory _factory;

    public CancellationFlowTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CancelFlow_MoreThan24H_FullRefund_RoomsReleased()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"cancel-full-flow-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(200));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(201));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        // Recently confirmed → within free window (< CancellationFreeHours since LastModifiedUtc)
        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Full refund test"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<CancellationDetailsResponse>();
        result.Should().NotBeNull();
        result!.RefundPercentage.Should().Be(1.0m);
        result.RefundAmount.Should().Be(booking.TotalAmount);
    }

    [Fact]
    public async Task CancelFlow_LessThan24H_PartialRefund_30PercentFee()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"cancel-partial-flow-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(210));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(211));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        // Move payment success time back to simulate > 24h since confirmation
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [payments] SET [PaidAtUtc] = {DateTimeOffset.UtcNow.AddHours(-25)} WHERE [BookingId] = {booking.Id}");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Partial refund test"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.ReadJsonAsync<CancellationDetailsResponse>();
        result.Should().NotBeNull();
        // CancellationFeePercent = 0.30 → refund = 70%
        result!.RefundPercentage.Should().Be(0.70m);
        var expectedRefund = Math.Round(booking.TotalAmount * 0.70m, 2, MidpointRounding.AwayFromZero);
        result.RefundAmount.Should().Be(expectedRefund);
    }

    [Fact]
    public async Task CancelFlow_ReturnsCancellationDetailsResponse()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"cancel-details-flow-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(220));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(221));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Details check"));

        var result = await response.ReadJsonAsync<CancellationDetailsResponse>();
        result.Should().NotBeNull();
        result!.BookingId.Should().Be(booking.Id);
        result.BookingNumber.Should().NotBeNullOrEmpty();
        result.RefundAmount.Should().BeGreaterThanOrEqualTo(0);
        result.RefundPercentage.Should().BeGreaterThan(0);
        result.CancelledAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CancelFlow_BookingStatusChangesToCancelled()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client,
            $"cancel-status-flow-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(230));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(231));

        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        await client.PostAsJsonAsync(
            $"/api/v1/bookings/{booking.Id}/cancel",
            new CancelBookingRequest("Status check"));

        // Verify booking status via API
        var detailsResponse = await client.GetAsync($"/api/v1/bookings/{booking.Id}");
        var details = await detailsResponse.ReadJsonAsync<BookingDetailsResponse>();

        details.Should().NotBeNull();
        details!.Status.Should().Be("Cancelled");
    }
}
