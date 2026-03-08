using FluentAssertions;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.CreateFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.DeleteFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.UpdateFeaturedDeal;
using HotelBooking.Application.Features.Events.Commands.TrackHotelView;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Hotels;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.Caching;

public sealed class FeaturedDealsCacheInvalidationTests
{
    [Fact]
    public async Task CreateFeaturedDeal_Success_InvalidatesHomeFeaturedDealsCache()
    {
        var db = new Mock<IAppDbContext>();
        var cacheInvalidator = new Mock<ICacheInvalidator>();

        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);

        db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.HotelRoomTypes).Returns(new List<HotelRoomType> { hrt }.AsQueryable().BuildMockDbSet().Object);

        var dealsSet = new List<FeaturedDeal>().AsQueryable().BuildMockDbSet();
        dealsSet.Setup(x => x.Add(It.IsAny<FeaturedDeal>()));
        db.Setup(x => x.FeaturedDeals).Returns(dealsSet.Object);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new CreateFeaturedDealCommandHandler(db.Object, cacheInvalidator.Object);

        var result = await sut.Handle(
            new CreateFeaturedDealCommand(hotel.Id, hrt.Id, 220m, 170m, 1, null, null),
            default);

        result.IsError.Should().BeFalse();
        cacheInvalidator.Verify(
            x => x.RemoveAsync("home:featured-deals", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateFeaturedDeal_Success_InvalidatesHomeFeaturedDealsCache()
    {
        var db = new Mock<IAppDbContext>();
        var cacheInvalidator = new Mock<ICacheInvalidator>();

        var hotel = TestHelpers.CreateHotel(name: "Deal Hotel");
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);
        var deal = new FeaturedDeal(Guid.NewGuid(), hotel.Id, hrt.Id, 300m, 250m, 1);
        TestHelpers.SetNav(deal, nameof(FeaturedDeal.Hotel), hotel);

        db.Setup(x => x.FeaturedDeals).Returns(new List<FeaturedDeal> { deal }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new UpdateFeaturedDealCommandHandler(db.Object, cacheInvalidator.Object);

        var result = await sut.Handle(
            new UpdateFeaturedDealCommand(deal.Id, 330m, 260m, 2, null, null),
            default);

        result.IsError.Should().BeFalse();
        cacheInvalidator.Verify(
            x => x.RemoveAsync("home:featured-deals", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFeaturedDeal_Success_InvalidatesHomeFeaturedDealsCache()
    {
        var db = new Mock<IAppDbContext>();
        var cacheInvalidator = new Mock<ICacheInvalidator>();

        var deal = new FeaturedDeal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 200m, 150m, 0);
        var dealsSet = new List<FeaturedDeal> { deal }.AsQueryable().BuildMockDbSet();
        dealsSet.Setup(x => x.Remove(It.IsAny<FeaturedDeal>()));
        db.Setup(x => x.FeaturedDeals).Returns(dealsSet.Object);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new DeleteFeaturedDealCommandHandler(db.Object, cacheInvalidator.Object);

        var result = await sut.Handle(new DeleteFeaturedDealCommand(deal.Id), default);

        result.IsError.Should().BeFalse();
        cacheInvalidator.Verify(
            x => x.RemoveAsync("home:featured-deals", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

public sealed class TrendingCitiesCacheInvalidationTests
{
    [Fact]
    public async Task TrackHotelView_NewVisit_InvalidatesTrendingCitiesCache()
    {
        var db = new Mock<IAppDbContext>();
        var cacheInvalidator = new Mock<ICacheInvalidator>();

        var hotel = TestHelpers.CreateHotel();
        db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);

        var visitsSet = new List<HotelVisit>().AsQueryable().BuildMockDbSet();
        visitsSet.Setup(x => x.Add(It.IsAny<HotelVisit>()));
        db.Setup(x => x.HotelVisits).Returns(visitsSet.Object);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new TrackHotelViewCommandHandler(db.Object, cacheInvalidator.Object);

        var result = await sut.Handle(new TrackHotelViewCommand(Guid.NewGuid(), hotel.Id), default);

        result.IsError.Should().BeFalse();
        cacheInvalidator.Verify(
            x => x.RemoveAsync("home:trending-cities", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TrackHotelView_DeduplicatedWithinWindow_DoesNotInvalidateTrendingCitiesCache()
    {
        var db = new Mock<IAppDbContext>();
        var cacheInvalidator = new Mock<ICacheInvalidator>();

        var userId = Guid.NewGuid();
        var hotel = TestHelpers.CreateHotel();
        var visit = new HotelVisit(Guid.NewGuid(), userId, hotel.Id);
        visit.UpdateVisitTime();

        db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.HotelVisits).Returns(new List<HotelVisit> { visit }.AsQueryable().BuildMockDbSet().Object);

        var sut = new TrackHotelViewCommandHandler(db.Object, cacheInvalidator.Object);

        var result = await sut.Handle(new TrackHotelViewCommand(userId, hotel.Id), default);

        result.IsError.Should().BeFalse();
        cacheInvalidator.Verify(
            x => x.RemoveAsync("home:trending-cities", It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
