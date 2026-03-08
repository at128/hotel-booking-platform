using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.IntegrationTests.Helpers;
using HotelBooking.Api.IntegrationTests.Infrastructure;
using HotelBooking.Contracts.Reviews;

using Xunit;
namespace HotelBooking.Api.IntegrationTests.Reviews;

[Collection("Integration")]
public class ReviewsTests
{
    private readonly WebAppFactory _factory;

    public ReviewsTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReview_AfterCompletedBooking_Returns201()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"review-ok-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(130));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(131));
        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/reviews",
            new CreateHotelReviewRequest(5, "Amazing", "Great stay!", booking.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var review = await response.ReadJsonAsync<ReviewDto>();
        review.Should().NotBeNull();
        review!.Rating.Should().Be(5);
    }

    [Fact]
    public async Task CreateReview_WithoutBooking_Returns403Or404()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"review-nobk-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/reviews",
            new CreateHotelReviewRequest(5, "Fake", "Never stayed",
                Guid.NewGuid())); // non-existent booking

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReview_InvalidRating_Returns400()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"review-bad-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/reviews",
            new CreateHotelReviewRequest(0, "Bad", "Invalid rating", Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReview_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/hotels/{Guid.NewGuid()}/reviews",
            new CreateHotelReviewRequest(5, "Test", "Test", Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReview_Duplicate_Returns409()
    {
        using var db = _factory.CreateDbContext();
        var seed = await SeedHelper.SeedFullHierarchy(db);
        var client = _factory.CreateClient();
        var auth = await AuthHelper.RegisterAndLogin(client, $"review-dup-{Guid.NewGuid():N}@test.com");
        AuthHelper.SetAuthToken(client, auth.Token.AccessToken);

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(140));
        var future2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(141));
        var booking = await SeedHelper.SeedConfirmedBooking(db, auth.Id, seed.Hotel,
            seed.HotelRoomType, seed.Rooms[0], future, future2);

        // First review
        await client.PostAsJsonAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/reviews",
            new CreateHotelReviewRequest(5, "Great", "Awesome", booking.Id));

        // Duplicate review for the same booking
        var response = await client.PostAsJsonAsync(
            $"/api/v1/hotels/{seed.Hotel.Id}/reviews",
            new CreateHotelReviewRequest(4, "Good", "Nice", booking.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
