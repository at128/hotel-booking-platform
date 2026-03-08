using FluentAssertions;
using HotelBooking.Application.Features.Search.Queries.SearchHotels;
using Xunit;

namespace HotelBooking.Application.Tests.Search;

public sealed class SearchValidatorTests
{
    [Theory]
    [InlineData((short)1)]
    [InlineData((short)5)]
    public async Task MinStarRating_BoundaryValues_ShouldPass(short rating)
    {
        var query = new SearchHotelsQuery(
            Query: null,
            City: null,
            RoomTypeId: null,
            CheckIn: null,
            CheckOut: null,
            Adults: null,
            Children: null,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: rating,
            Amenities: null,
            SortBy: null,
            Cursor: null,
            Limit: 20);

        var validator = new SearchHotelsQueryValidator();
        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(25)]
    public async Task Adults_InvalidValues_ShouldFail(int adults)
    {
        var query = new SearchHotelsQuery(
            Query: null,
            City: null,
            RoomTypeId: null,
            CheckIn: null,
            CheckOut: null,
            Adults: adults,
            Children: null,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: null,
            Amenities: null,
            SortBy: null,
            Cursor: null,
            Limit: 20);

        var validator = new SearchHotelsQueryValidator();
        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(20)]
    public async Task Adults_ValidValues_ShouldPass(int adults)
    {
        var query = new SearchHotelsQuery(
            Query: null,
            City: null,
            RoomTypeId: null,
            CheckIn: null,
            CheckOut: null,
            Adults: adults,
            Children: null,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: null,
            Amenities: null,
            SortBy: null,
            Cursor: null,
            Limit: 20);

        var validator = new SearchHotelsQueryValidator();
        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Children_InvalidValues_ShouldFail(int children)
    {
        var query = new SearchHotelsQuery(
            Query: null,
            City: null,
            RoomTypeId: null,
            CheckIn: null,
            CheckOut: null,
            Adults: null,
            Children: children,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: null,
            Amenities: null,
            SortBy: null,
            Cursor: null,
            Limit: 20);

        var validator = new SearchHotelsQueryValidator();
        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public async Task Children_ValidValues_ShouldPass(int children)
    {
        var query = new SearchHotelsQuery(
            Query: null,
            City: null,
            RoomTypeId: null,
            CheckIn: null,
            CheckOut: null,
            Adults: null,
            Children: children,
            NumberOfRooms: null,
            MinPrice: null,
            MaxPrice: null,
            MinStarRating: null,
            Amenities: null,
            SortBy: null,
            Cursor: null,
            Limit: 20);

        var validator = new SearchHotelsQueryValidator();
        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }
}