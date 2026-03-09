/// <summary>
/// Tests for Admin City command and query handlers.
/// </summary>
using FluentAssertions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Admin.Cities.Command.CreateCity;
using HotelBooking.Application.Features.Admin.Cities.Command.DeleteCity;
using HotelBooking.Application.Features.Admin.Cities.Command.UpdateCity;
using HotelBooking.Application.Features.Admin.Cities.Queries.GetCities;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;
namespace HotelBooking.Application.Tests.Admin.Cities;

public class CreateCityCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    private void SetupCities(List<City> cities)
    {
        var mock = cities.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);
    }

    [Fact]
    public async Task Handle_NewCity_ReturnsCreatedDto()
    {
        // Arrange
        SetupCities([]);
        _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var handler = new CreateCityCommandHandler(_db.Object);
        var cmd = new CreateCityCommand("Amman", "Jordan", "11180");

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Amman");
        result.Value.Country.Should().Be("Jordan");
        result.Value.PostOffice.Should().Be("11180");
    }

    [Fact]
    public async Task Handle_DuplicateCity_ReturnsAlreadyExists()
    {
        // Arrange
        var existing = TestHelpers.CreateCity(name: "Amman", country: "Jordan");
        SetupCities([existing]);

        var handler = new CreateCityCommandHandler(_db.Object);
        var cmd = new CreateCityCommand("Amman", "Jordan", null);

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Cities.AlreadyExists.Code);
    }

    [Fact]
    public async Task Handle_DbUniqueViolation_ReturnsAlreadyExists()
    {
        // Arrange
        SetupCities([]);
        _db.Setup(x => x.SaveChangesAsync(default))
            .ThrowsAsync(new DbUpdateException("cities unique", new Exception("IX_cities_Name_Country")));

        var handler = new CreateCityCommandHandler(_db.Object);
        var cmd = new CreateCityCommand("Amman", "Jordan", null);

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Cities.AlreadyExists.Code);
    }
}

public class UpdateCityCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_ExistingCity_UpdatesAndReturnsDto()
    {
        // Arrange
        var cityId = Guid.NewGuid();
        var city = TestHelpers.CreateCity(id: cityId, name: "Old", country: "OC");
        var mock = new List<City> { city }.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);
        _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var handler = new UpdateCityCommandHandler(_db.Object);
        var cmd = new UpdateCityCommand(cityId, "New Name", "New Country", null);

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("New Name");
        result.Value.Country.Should().Be("New Country");
    }

    [Fact]
    public async Task Handle_CityNotFound_ReturnsNotFound()
    {
        // Arrange
        var mock = new List<City>().AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);

        var handler = new UpdateCityCommandHandler(_db.Object);
        var cmd = new UpdateCityCommand(Guid.NewGuid(), "Name", "Country", null);

        // Act
        var result = await handler.Handle(cmd, default);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Cities.NotFound.Code);
    }

    [Fact]
    public async Task Handle_DuplicateNameCountry_ReturnsAlreadyExists()
    {
        var cityId = Guid.NewGuid();
        var city = TestHelpers.CreateCity(id: cityId, name: "Amman", country: "Jordan");
        var dup = TestHelpers.CreateCity(name: "New Name", country: "New Country");

        var mock = new List<City> { city, dup }.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);

        var handler = new UpdateCityCommandHandler(_db.Object);
        var cmd = new UpdateCityCommand(cityId, "New Name", "New Country", "11180");

        var result = await handler.Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Cities.AlreadyExists.Code);
    }

    [Fact]
    public async Task Handle_DbUniqueViolation_ReturnsAlreadyExists()
    {
        var cityId = Guid.NewGuid();
        var city = TestHelpers.CreateCity(id: cityId, name: "Old", country: "OC");
        var mock = new List<City> { city }.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);
        _db.Setup(x => x.SaveChangesAsync(default))
            .ThrowsAsync(new DbUpdateException("cities unique", new Exception("IX_cities_Name_Country")));

        var handler = new UpdateCityCommandHandler(_db.Object);
        var cmd = new UpdateCityCommand(cityId, "New Name", "New Country", null);

        var result = await handler.Handle(cmd, default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Cities.AlreadyExists.Code);
    }
}

public class DeleteCityCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_CityWithNoHotels_SoftDeletes()
    {
        // Arrange
        var cityId = Guid.NewGuid();
        var city = TestHelpers.CreateCity(id: cityId);
        var mock = new List<City> { city }.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);
        _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var handler = new DeleteCityCommandHandler(_db.Object);

        // Act
        var result = await handler.Handle(new DeleteCityCommand(cityId), default);

        // Assert
        result.IsError.Should().BeFalse();
        city.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_CityNotFound_ReturnsNotFound()
    {
        // Arrange
        var mock = new List<City>().AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);

        var handler = new DeleteCityCommandHandler(_db.Object);

        // Act
        var result = await handler.Handle(new DeleteCityCommand(Guid.NewGuid()), default);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Cities.NotFound.Code);
    }

    [Fact]
    public async Task Handle_CityWithActiveHotels_ReturnsConflict()
    {
        // Arrange
        var cityId = Guid.NewGuid();
        var city = TestHelpers.CreateCity(id: cityId);
        var hotel = TestHelpers.CreateHotel(cityId: cityId);
        // Navigate hotel into city
        typeof(City).GetProperty("Hotels")!
            .SetValue(city, new List<Hotel> { hotel });

        var mock = new List<City> { city }.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);

        var handler = new DeleteCityCommandHandler(_db.Object);

        // Act
        var result = await handler.Handle(new DeleteCityCommand(cityId), default);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(AdminErrors.Cities.HasRelatedHotels.Code);
    }
}

public class GetCitiesQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_ReturnsPaginatedCities()
    {
        // Arrange
        var cities = new List<City>
        {
            TestHelpers.CreateCity(name: "Amman"),
            TestHelpers.CreateCity(name: "Dubai"),
        };
        var mock = cities.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);

        var handler = new GetCitiesQueryHandler(_db.Object);
        var query = new GetCitiesQuery(Search: null, Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Items.Count.Should().Be(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithSearch_FiltersResults()
    {
        // Arrange
        var cities = new List<City>
        {
            TestHelpers.CreateCity(name: "Amman"),
            TestHelpers.CreateCity(name: "Dubai"),
        };
        var mock = cities.AsQueryable().BuildMockDbSet();
        _db.Setup(x => x.Cities).Returns(mock.Object);

        var handler = new GetCitiesQueryHandler(_db.Object);
        var query = new GetCitiesQuery(Search: "Amman", Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Items.Should().OnlyContain(c => c.Name.Contains("Amman"));
    }
}
