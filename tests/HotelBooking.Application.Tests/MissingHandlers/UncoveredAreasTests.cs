using FluentAssertions;
using FluentValidation.TestHelper;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.CreateFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.DeleteFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Commands.UpdateFeaturedDeal;
using HotelBooking.Application.Features.Admin.FeaturedDeals.Queries.GetAdminFeaturedDeals;
using HotelBooking.Application.Features.Admin.Hotels.Commands.DeleteHotelImage;
using HotelBooking.Application.Features.Admin.Hotels.Commands.LinkService;
using HotelBooking.Application.Features.Admin.Hotels.Commands.SetHotelThumbnail;
using HotelBooking.Application.Features.Admin.Hotels.Commands.UnlinkService;
using HotelBooking.Application.Features.Admin.Hotels.Commands.UpdateHotelImage;
using HotelBooking.Application.Features.Admin.Payments.Queries.GetAdminPayments;
using HotelBooking.Application.Features.Admin.Rooms.Quries.GetRoomById;
using HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypeById;
using HotelBooking.Application.Features.Admin.Services.Queries.GetServiceById;
using HotelBooking.Application.Features.Reviews.Commands.DeleteReview;
using HotelBooking.Application.Features.Reviews.Commands.UpdateReview;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Bookings.Enums;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Hotels.Enums;
using HotelBooking.Domain.Reviews;
using HotelBooking.Domain.Rooms;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.MissingHandlers;

public sealed class CreateFeaturedDealMissingTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task Handle_HotelNotFound_ReturnsError()
    {
        _db.Setup(x => x.Hotels).Returns(new List<Hotel>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelRoomTypes).Returns(new List<HotelRoomType>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.FeaturedDeals).Returns(new List<FeaturedDeal>().AsQueryable().BuildMockDbSet().Object);

        var sut = new CreateFeaturedDealCommandHandler(_db.Object);
        var result = await sut.Handle(new CreateFeaturedDealCommand(Guid.NewGuid(), Guid.NewGuid(), 200, 150, 1, null, null), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Success_ReturnsDto()
    {
        var hotel = TestHelpers.CreateHotel();
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);

        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelRoomTypes).Returns(new List<HotelRoomType> { hrt }.AsQueryable().BuildMockDbSet().Object);

        var dealSet = new List<FeaturedDeal>().AsQueryable().BuildMockDbSet();
        dealSet.Setup(x => x.Add(It.IsAny<FeaturedDeal>()));
        _db.Setup(x => x.FeaturedDeals).Returns(dealSet.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new CreateFeaturedDealCommandHandler(_db.Object);
        var result = await sut.Handle(new CreateFeaturedDealCommand(hotel.Id, hrt.Id, 250, 200, 2, null, null), default);

        result.IsError.Should().BeFalse();
        result.Value.HotelName.Should().Be(hotel.Name);
    }
}

public sealed class CreateFeaturedDealCommandValidatorTestsMissing
{
    private readonly CreateFeaturedDealCommandValidator _v = new();

    [Fact]
    public void Valid_NoErrors()
    {
        var cmd = new CreateFeaturedDealCommand(Guid.NewGuid(), Guid.NewGuid(), 300, 200, 0, null, null);
        _v.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DiscountedPrice_GreaterThanOriginal_Error()
    {
        var cmd = new CreateFeaturedDealCommand(Guid.NewGuid(), Guid.NewGuid(), 300, 350, 0, null, null);
        _v.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DiscountedPrice);
    }
}

public sealed class FeaturedDealsMissingTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task UpdateFeaturedDeal_NotFound_ReturnsError()
    {
        _db.Setup(x => x.FeaturedDeals).Returns(new List<FeaturedDeal>().AsQueryable().BuildMockDbSet().Object);

        var sut = new UpdateFeaturedDealCommandHandler(_db.Object);
        var result = await sut.Handle(new UpdateFeaturedDealCommand(Guid.NewGuid(), 300, 250, 1, null, null), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateFeaturedDeal_Success_UpdatesValues()
    {
        var hotel = TestHelpers.CreateHotel(name: "Deal Hotel");
        var roomType = TestHelpers.CreateRoomType();
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, roomType);
        var deal = new FeaturedDeal(Guid.NewGuid(), hotel.Id, hrt.Id, 300, 250, 1);
        TestHelpers.SetNav(deal, nameof(FeaturedDeal.Hotel), hotel);

        _db.Setup(x => x.FeaturedDeals).Returns(new List<FeaturedDeal> { deal }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new UpdateFeaturedDealCommandHandler(_db.Object);
        var result = await sut.Handle(new UpdateFeaturedDealCommand(deal.Id, 400, 320, 3, null, null), default);

        result.IsError.Should().BeFalse();
        result.Value.DiscountedPrice.Should().Be(320);
    }

    [Fact]
    public async Task DeleteFeaturedDeal_NotFound_ReturnsError()
    {
        _db.Setup(x => x.FeaturedDeals).Returns(new List<FeaturedDeal>().AsQueryable().BuildMockDbSet().Object);

        var sut = new DeleteFeaturedDealCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteFeaturedDealCommand(Guid.NewGuid()), default);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFeaturedDeal_Success_ReturnsDeleted()
    {
        var deal = new FeaturedDeal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 200, 150, 0);
        var set = new List<FeaturedDeal> { deal }.AsQueryable().BuildMockDbSet();
        set.Setup(x => x.Remove(It.IsAny<FeaturedDeal>()));
        _db.Setup(x => x.FeaturedDeals).Returns(set.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new DeleteFeaturedDealCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteFeaturedDealCommand(deal.Id), default);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task GetAdminFeaturedDeals_ReturnsFilteredPage()
    {
        var hotelA = TestHelpers.CreateHotel(name: "Alpha Hotel");
        var hotelB = TestHelpers.CreateHotel(name: "Bravo Hotel");
        var rt = TestHelpers.CreateRoomType();
        var hrtA = TestHelpers.CreateHotelRoomTypeFor(hotelA, rt);
        var hrtB = TestHelpers.CreateHotelRoomTypeFor(hotelB, rt);

        var deals = new List<FeaturedDeal>
        {
            new(Guid.NewGuid(), hotelA.Id, hrtA.Id, 200, 150, 1),
            new(Guid.NewGuid(), hotelB.Id, hrtB.Id, 300, 220, 2)
        };
        TestHelpers.SetNav(deals[0], nameof(FeaturedDeal.Hotel), hotelA);
        TestHelpers.SetNav(deals[1], nameof(FeaturedDeal.Hotel), hotelB);
        TestHelpers.SetNav(deals[0], nameof(FeaturedDeal.HotelRoomType), hrtA);
        TestHelpers.SetNav(deals[1], nameof(FeaturedDeal.HotelRoomType), hrtB);

        _db.Setup(x => x.FeaturedDeals).Returns(deals.AsQueryable().BuildMockDbSet().Object);

        var sut = new GetAdminFeaturedDealsQueryHandler(_db.Object);
        var result = await sut.Handle(new GetAdminFeaturedDealsQuery("alpha", 1, 10), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].HotelName.Should().Contain("Alpha");
    }
}

public sealed class AdminHotelImageAndServiceMissingTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task DeleteHotelImage_Success_ClearsThumbnail()
    {
        var hotel = TestHelpers.CreateHotel();
        var image = new Image(Guid.NewGuid(), ImageType.Hotel, hotel.Id, "https://img.test/h.jpg");
        hotel.SetThumbnail(image.Url);

        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        var images = new List<Image> { image }.AsQueryable().BuildMockDbSet();
        images.Setup(x => x.Remove(It.IsAny<Image>()));
        _db.Setup(x => x.Images).Returns(images.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new DeleteHotelImageCommandHandler(_db.Object);
        var result = await sut.Handle(new DeleteHotelImageCommand(hotel.Id, image.Id), default);

        result.IsError.Should().BeFalse();
        hotel.ThumbnailUrl.Should().BeNull();
    }

    [Fact]
    public async Task SetHotelThumbnail_Success_UpdatesHotel()
    {
        var hotel = TestHelpers.CreateHotel();
        var image = new Image(Guid.NewGuid(), ImageType.Hotel, hotel.Id, "https://img.test/new.jpg");

        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Images).Returns(new List<Image> { image }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new SetHotelThumbnailCommandHandler(_db.Object);
        var result = await sut.Handle(new SetHotelThumbnailCommand(hotel.Id, image.Id), default);

        result.IsError.Should().BeFalse();
        hotel.ThumbnailUrl.Should().Be(image.Url);
    }

    [Fact]
    public async Task UpdateHotelImage_Success_ReturnsDto()
    {
        var hotel = TestHelpers.CreateHotel();
        var image = new Image(Guid.NewGuid(), ImageType.Hotel, hotel.Id, "https://img.test/a.jpg", "old", 0);

        _db.Setup(x => x.Images).Returns(new List<Image> { image }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new UpdateHotelImageCommandHandler(_db.Object);
        var result = await sut.Handle(new UpdateHotelImageCommand(hotel.Id, image.Id, "new", 2), default);

        result.IsError.Should().BeFalse();
        result.Value.Caption.Should().Be("new");
        result.Value.SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task LinkAndUnlinkService_Success()
    {
        var hotel = TestHelpers.CreateHotel();
        var service = TestHelpers.CreateService();
        var hsSet = new List<HotelService>().AsQueryable().BuildMockDbSet();
        hsSet.Setup(x => x.Add(It.IsAny<HotelService>()));
        hsSet.Setup(x => x.Remove(It.IsAny<HotelService>()));

        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Services).Returns(new List<Domain.Services.Service> { service }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelServices).Returns(hsSet.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var link = new LinkServiceToHotelCommandHandler(_db.Object);
        var linkResult = await link.Handle(new LinkServiceToHotelCommand(hotel.Id, service.Id, 10m, false), default);
        linkResult.IsError.Should().BeFalse();

        var hs = new HotelService(Guid.NewGuid(), hotel.Id, service.Id, 10m, false);
        _db.Setup(x => x.HotelServices).Returns(new List<HotelService> { hs }.AsQueryable().BuildMockDbSet().Object);

        var unlink = new UnlinkServiceFromHotelCommandHandler(_db.Object);
        var unlinkResult = await unlink.Handle(new UnlinkServiceFromHotelCommand(hotel.Id, service.Id), default);
        unlinkResult.IsError.Should().BeFalse();
    }
}

public sealed class AdminQueriesMissingTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task GetAdminPayments_ReturnsFilteredItems()
    {
        var booking = TestHelpers.CreateBooking(bookingNumber: "BK-ABC-001");
        var payment = TestHelpers.CreatePayment(bookingId: booking.Id, status: PaymentStatus.Succeeded);
        TestHelpers.SetNav(payment, nameof(Payment.Booking), booking);

        _db.Setup(x => x.Payments).Returns(new List<Payment> { payment }.AsQueryable().BuildMockDbSet().Object);

        var sut = new GetAdminPaymentsQueryHandler(_db.Object);
        var result = await sut.Handle(new GetAdminPaymentsQuery("Succeeded", "ABC", 1, 20), default);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetRoomById_Success_ReturnsDto()
    {
        var hotel = TestHelpers.CreateHotel(name: "R Hotel");
        var rt = TestHelpers.CreateRoomType(name: "Suite");
        var hrt = TestHelpers.CreateHotelRoomTypeFor(hotel, rt);
        var room = TestHelpers.CreateRoom(hotelRoomTypeId: hrt.Id, hotelId: hotel.Id, roomNumber: "909");
        TestHelpers.SetNav(room, nameof(Room.Hotel), hotel);
        TestHelpers.SetNav(room, nameof(Room.HotelRoomType), hrt);
        TestHelpers.SetNav(hrt, nameof(HotelRoomType.RoomType), rt);

        _db.Setup(x => x.Rooms).Returns(new List<Room> { room }.AsQueryable().BuildMockDbSet().Object);

        var sut = new GetRoomByIdQueryHandler(_db.Object);
        var result = await sut.Handle(new GetRoomByIdQuery(room.Id), default);

        result.IsError.Should().BeFalse();
        result.Value.RoomNumber.Should().Be("909");
    }

    [Fact]
    public async Task GetRoomTypeById_And_GetServiceById_Success()
    {
        var rt = TestHelpers.CreateRoomType(name: "Family");
        var hrt = TestHelpers.CreateHotelRoomType(hotelId: Guid.NewGuid(), roomTypeId: rt.Id);
        rt.HotelRoomTypes.Add(hrt);

        var service = TestHelpers.CreateService(name: "Spa");
        var hs = new HotelService(Guid.NewGuid(), Guid.NewGuid(), service.Id, 25, false);
        service.HotelServices.Add(hs);

        _db.Setup(x => x.RoomTypes).Returns(new List<RoomType> { rt }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Services).Returns(new List<Domain.Services.Service> { service }.AsQueryable().BuildMockDbSet().Object);

        var roomTypeResult = await new GetRoomTypeByIdQueryHandler(_db.Object)
            .Handle(new GetRoomTypeByIdQuery(rt.Id), default);
        var serviceResult = await new GetServiceByIdQueryHandler(_db.Object)
            .Handle(new GetServiceByIdQuery(service.Id), default);

        roomTypeResult.IsError.Should().BeFalse();
        serviceResult.IsError.Should().BeFalse();
    }
}

public sealed class ReviewCommandMissingTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task UpdateReview_Success_And_DeleteByAdmin_Success()
    {
        var userId = Guid.NewGuid();
        var review = new Review(Guid.NewGuid(), userId, Guid.NewGuid(), Guid.NewGuid(), 4, "old", "old");
        _db.Setup(x => x.Reviews).Returns(new List<Review> { review }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var update = new UpdateReviewCommandHandler(_db.Object);
        var updateResult = await update.Handle(new UpdateReviewCommand(review.HotelId, review.Id, userId, 5, "new", "updated"), default);
        updateResult.IsError.Should().BeFalse();
        updateResult.Value.Rating.Should().Be(5);

        var delete = new DeleteReviewCommandHandler(_db.Object);
        var deleteResult = await delete.Handle(new DeleteReviewCommand(review.HotelId, review.Id, Guid.NewGuid(), true), default);
        deleteResult.IsError.Should().BeFalse();
        review.DeletedAtUtc.Should().NotBeNull();
    }
}

public sealed class RecordAndModelCoverageTests
{
    [Fact]
    public void InterfaceRecords_AndPaginatedList_AreConstructible()
    {
        var hold = new ActiveHoldDto(Guid.NewGuid(), Guid.NewGuid(), "Deluxe", 2, DateTimeOffset.UtcNow.AddMinutes(15));
        var roomItem = new BookingRoomEmailItem("Deluxe", "101", 120m, "USD");
        var request = new HotelSearchRequest("q", "city", null, null, null, 2, 0, 1, null, null, null, null, null, null, 20);
        var doc = new HotelSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = "Hotel",
            CityName = "City",
            Country = "Country",
            CityId = Guid.NewGuid(),
            Owner = "Owner",
            StarRating = 4,
            Amenities = ["Wifi"],
            RoomTypes = [new RoomTypeInfo(Guid.NewGuid(), "Deluxe", 100m, 2, 1, 4)]
        };
        var list = new PaginatedList<int> { PageNumber = 1, PageSize = 10, TotalCount = 1, TotalPages = 1, Items = [1] };

        hold.RoomTypeName.Should().Be("Deluxe");
        roomItem.RoomNumber.Should().Be("101");
        request.Limit.Should().Be(20);
        doc.RoomTypes.Should().HaveCount(1);
        list.Items.Should().ContainSingle();
    }
}
