using FluentAssertions;
using FluentValidation.TestHelper;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Models.Payment;
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
using HotelBooking.Application.Features.Events.Commands.TrackHotelView;
using HotelBooking.Application.Features.Reviews.Commands.CreateHotelReview;
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
        var hotel = TestHelpers.CreateHotel(id: review.HotelId);
        _db.Setup(x => x.Reviews).Returns(new List<Review> { review }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
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
        var request = new HotelSearchRequest("q", "city", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), 2, 1, 1, 100m, 400m, 4, new List<string> { "Spa" }, "rating_desc", "cursor", 20);
        var doc = new HotelSearchDocument
        {
            Id = Guid.NewGuid(),
            Name = "Hotel",
            CityName = "City",
            Country = "Country",
            CityId = Guid.NewGuid(),
            Description = "Desc",
            Owner = "Owner",
            StarRating = 4,
            MinPricePerNight = 100m,
            AverageRating = 4.2,
            ReviewCount = 11,
            ThumbnailUrl = "thumb.jpg",
            Amenities = ["Wifi"],
            RoomTypes = [new RoomTypeInfo(Guid.NewGuid(), "Deluxe", 100m, 2, 1, 4)],
            SearchableText = "Hotel City Desc Wifi",
            Embedding = [0.1f, 0.2f]
        };

        var roomTypeInfo = doc.RoomTypes[0];
        var emailData = new BookingConfirmationEmailData(
            "BK-1",
            "Hotel",
            "Address",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            2,
            250m,
            "USD",
            "txn_1",
            new List<BookingRoomEmailItem> { roomItem });

        var paymentReq = new PaymentSessionRequest(
            Guid.NewGuid(),
            "BK-2",
            300m,
            "u@test.com",
            "Hotel",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)),
            "https://ok",
            "https://cancel");

        var list = new PaginatedList<int> { PageNumber = 1, PageSize = 10, TotalCount = 1, TotalPages = 1, Items = [1] };

        hold.Id.Should().NotBe(Guid.Empty);
        hold.HotelRoomTypeId.Should().NotBe(Guid.Empty);
        hold.Quantity.Should().Be(2);
        hold.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(10));
        hold.RoomTypeName.Should().Be("Deluxe");

        roomItem.RoomTypeName.Should().Be("Deluxe");
        roomItem.RoomNumber.Should().Be("101");
        roomItem.PricePerNight.Should().Be(120m);
        roomItem.Currency.Should().Be("USD");

        request.Query.Should().Be("q");
        request.City.Should().Be("city");
        request.RoomTypeId.Should().NotBeNull();
        request.CheckIn.Should().NotBeNull();
        request.CheckOut.Should().NotBeNull();
        request.Adults.Should().Be(2);
        request.Children.Should().Be(1);
        request.NumberOfRooms.Should().Be(1);
        request.MinPrice.Should().Be(100m);
        request.MaxPrice.Should().Be(400m);
        request.MinStarRating.Should().Be(4);
        request.Amenities.Should().ContainSingle("Spa");
        request.SortBy.Should().Be("rating_desc");
        request.Cursor.Should().Be("cursor");
        request.Limit.Should().Be(20);

        doc.Id.Should().NotBe(Guid.Empty);
        doc.Name.Should().Be("Hotel");
        doc.CityName.Should().Be("City");
        doc.Country.Should().Be("Country");
        doc.CityId.Should().NotBe(Guid.Empty);
        doc.Description.Should().Be("Desc");
        doc.Owner.Should().Be("Owner");
        doc.StarRating.Should().Be(4);
        doc.MinPricePerNight.Should().Be(100m);
        doc.AverageRating.Should().Be(4.2);
        doc.ReviewCount.Should().Be(11);
        doc.ThumbnailUrl.Should().Be("thumb.jpg");
        doc.Amenities.Should().Contain("Wifi");
        roomTypeInfo.RoomTypeId.Should().NotBe(Guid.Empty);
        roomTypeInfo.RoomTypeName.Should().Be("Deluxe");
        roomTypeInfo.PricePerNight.Should().Be(100m);
        roomTypeInfo.AdultCapacity.Should().Be(2);
        roomTypeInfo.ChildCapacity.Should().Be(1);
        roomTypeInfo.AvailableRoomCount.Should().Be(4);
        doc.SearchableText.Should().Contain("Wifi");
        doc.Embedding.Should().HaveCount(2);

        emailData.BookingNumber.Should().Be("BK-1");
        emailData.HotelName.Should().Be("Hotel");
        emailData.HotelAddress.Should().Be("Address");
        emailData.CheckIn.Should().BeBefore(emailData.CheckOut);
        emailData.Nights.Should().Be(2);
        emailData.TotalAmount.Should().Be(250m);
        emailData.Currency.Should().Be("USD");
        emailData.TransactionRef.Should().Be("txn_1");
        emailData.Rooms.Should().ContainSingle();

        paymentReq.BookingId.Should().NotBe(Guid.Empty);
        paymentReq.BookingNumber.Should().Be("BK-2");
        paymentReq.AmountInUsd.Should().Be(300m);
        paymentReq.CustomerEmail.Should().Be("u@test.com");
        paymentReq.HotelName.Should().Be("Hotel");
        paymentReq.CheckIn.Should().BeBefore(paymentReq.CheckOut);
        paymentReq.SuccessUrl.Should().Contain("ok");
        paymentReq.CancelUrl.Should().Contain("cancel");

        doc.RoomTypes.Should().HaveCount(1);
        list.Items.Should().ContainSingle();
    }

    [Fact]
    public void ApplicationErrors_StaticMembers_AreReachable()
    {
        ApplicationErrors.Auth.EmailAlreadyRegistered.Code.Should().Be("Auth.EmailAlreadyRegistered");
        ApplicationErrors.Auth.RegistrationFailed("x").Code.Should().Be("Auth.RegistrationFailed");
        ApplicationErrors.Auth.PasswordChangeFailed("x").Code.Should().Be("Auth.PasswordChangeFailed");

        ApplicationErrors.Cart.InvalidQuantity(5).Code.Should().Be("Cart.InvalidQuantity");
        ApplicationErrors.Checkout.RoomUnavailable("Deluxe").Code.Should().Be("Checkout.RoomUnavailable");
        ApplicationErrors.Payment.RoomNoLongerAvailable("Suite").Code.Should().Be("Payment.RoomNoLongerAvailable");
        ApplicationErrors.Booking.AccessDenied.Code.Should().Be("Booking.AccessDenied");
    }
}

public sealed class TrackHotelViewAndReviewCoverageTests
{
    private readonly Mock<IAppDbContext> _db = new();

    [Fact]
    public async Task TrackHotelView_ExistingVisitWithinWindow_NoUpdate()
    {
        var userId = Guid.NewGuid();
        var hotel = TestHelpers.CreateHotel();
        var visit = new HotelVisit(Guid.NewGuid(), userId, hotel.Id);
        visit.UpdateVisitTime();

        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelVisits).Returns(new List<HotelVisit> { visit }.AsQueryable().BuildMockDbSet().Object);

        var cache = new Mock<ICacheInvalidator>();
        var sut = new TrackHotelViewCommandHandler(_db.Object, cache.Object);

        var result = await sut.Handle(new TrackHotelViewCommand(userId, hotel.Id), default);

        result.IsError.Should().BeFalse();
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TrackHotelView_ExistingVisitOlderThanWindow_UpdatesAndInvalidatesCache()
    {
        var userId = Guid.NewGuid();
        var hotel = TestHelpers.CreateHotel();
        var visit = new HotelVisit(Guid.NewGuid(), userId, hotel.Id);
        TestHelpers.SetPrivateProp(visit, nameof(HotelVisit.VisitedAtUtc), DateTimeOffset.UtcNow.AddMinutes(-10));

        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.HotelVisits).Returns(new List<HotelVisit> { visit }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cache = new Mock<ICacheInvalidator>();
        var sut = new TrackHotelViewCommandHandler(_db.Object, cache.Object);

        var result = await sut.Handle(new TrackHotelViewCommand(userId, hotel.Id), default);

        result.IsError.Should().BeFalse();
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync("home:trending-cities", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHotelReview_Branches_AreCovered()
    {
        var userId = Guid.NewGuid();
        var hotel = TestHelpers.CreateHotel();
        var booking = TestHelpers.CreateBooking(userId: userId, hotelId: hotel.Id, status: BookingStatus.Confirmed);

        _db.Setup(x => x.Bookings).Returns(new List<Booking> { booking }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Reviews).Returns(new List<Review>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Hotels).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new CreateHotelReviewCommandHandler(_db.Object);
        var cmd = new CreateHotelReviewCommand(hotel.Id, booking.Id, userId, 5, " Great ", " Nice ");

        var result = await sut.Handle(cmd, default);

        result.IsError.Should().BeFalse();
        result.Value.Title.Should().Be("Great");
        result.Value.Comment.Should().Be("Nice");
    }

    [Fact]
    public async Task CreateHotelReview_HotelMismatch_ReturnsValidation()
    {
        var userId = Guid.NewGuid();
        var booking = TestHelpers.CreateBooking(userId: userId, hotelId: Guid.NewGuid(), status: BookingStatus.Confirmed);

        _db.Setup(x => x.Bookings).Returns(new List<Booking> { booking }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Reviews).Returns(new List<Review>().AsQueryable().BuildMockDbSet().Object);

        var sut = new CreateHotelReviewCommandHandler(_db.Object);
        var result = await sut.Handle(
            new CreateHotelReviewCommand(Guid.NewGuid(), booking.Id, userId, 4, null, null), default);

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Reviews.BookingHotelMismatch");
    }
}
