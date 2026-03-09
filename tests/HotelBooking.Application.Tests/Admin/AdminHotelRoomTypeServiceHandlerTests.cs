/// <summary>
/// Tests for Admin Hotel, RoomType, and Service command/query handlers.
/// </summary>
using FluentAssertions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Admin.Hotels.Command.CreateHotel;
using HotelBooking.Application.Features.Admin.RoomTypes.Commands.CreateRoomType;
using HotelBooking.Application.Features.Admin.RoomTypes.Commands.DeleteRoomType;
using HotelBooking.Application.Features.Admin.RoomTypes.Commands.UpdateRoomType;
using HotelBooking.Application.Features.Admin.RoomTypes.Queries.GetRoomTypes;
using HotelBooking.Application.Features.Admin.Services.Commands.CreateService;
using HotelBooking.Application.Features.Admin.Services.Commands.DeleteService;
using HotelBooking.Application.Features.Admin.Services.Commands.UpdateService;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using HotelBooking.Domain.Services;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;
namespace HotelBooking.Application.Tests.Admin.Hotels
{

    public class CreateHotelCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        private void SetupCities(List<City> cities)
        {
            _db.Setup(x => x.Cities).Returns(cities.AsQueryable().BuildMockDbSet().Object);
        }

        private void SetupHotels(List<Hotel> hotels)
        {
            _db.Setup(x => x.Hotels).Returns(hotels.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        }

        private static CreateHotelCommand ValidCmd(Guid? cityId = null) =>
            new(cityId ?? Guid.NewGuid(), "Grand Palace", "John Doe", "1 King St", 5, null, null, null);

        [Fact]
        public async Task Handle_Success_CreatesHotel()
        {
            var cityId = Guid.NewGuid();
            var city = TestHelpers.CreateCity(id: cityId);
            SetupCities([city]);
            SetupHotels([]);

            var result = await new CreateHotelCommandHandler(_db.Object)
                .Handle(ValidCmd(cityId), default);

            result.IsError.Should().BeFalse();
            result.Value.Name.Should().Be("Grand Palace");
            result.Value.CityId.Should().Be(cityId);
        }

        [Fact]
        public async Task Handle_CityNotFound_ReturnsError()
        {
            SetupCities([]);
            SetupHotels([]);

            var result = await new CreateHotelCommandHandler(_db.Object)
                .Handle(ValidCmd(), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.Cities.NotFound.Code);
        }

        [Fact]
        public async Task Handle_DuplicateNameInCity_ReturnsConflict()
        {
            var cityId = Guid.NewGuid();
            var city = TestHelpers.CreateCity(id: cityId);
            var existingHotel = TestHelpers.CreateHotel(cityId: cityId, name: "Grand Palace");
            SetupCities([city]);
            SetupHotels([existingHotel]);

            var result = await new CreateHotelCommandHandler(_db.Object)
                .Handle(ValidCmd(cityId), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.Hotels.AlreadyExists.Code);
        }
    }

}
namespace HotelBooking.Application.Tests.Admin.RoomTypes
{

    public class CreateRoomTypeCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        [Fact]
        public async Task Handle_Success_CreatesRoomType()
        {
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType>().AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await new CreateRoomTypeCommandHandler(_db.Object)
                .Handle(new CreateRoomTypeCommand("Deluxe", "Spacious room"), default);

            result.IsError.Should().BeFalse();
            result.Value.Name.Should().Be("Deluxe");
        }

        [Fact]
        public async Task Handle_DuplicateName_ReturnsAlreadyExists()
        {
            var existing = TestHelpers.CreateRoomType(name: "Deluxe");
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType> { existing }.AsQueryable().BuildMockDbSet().Object);

            var result = await new CreateRoomTypeCommandHandler(_db.Object)
                .Handle(new CreateRoomTypeCommand("Deluxe", null), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.RoomTypes.AlreadyExists.Code);
        }

        [Fact]
        public async Task Handle_DbUniqueViolation_ReturnsAlreadyExists()
        {
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType>().AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("room_types unique", new Exception("IX_room_types_Name")));

            var result = await new CreateRoomTypeCommandHandler(_db.Object)
                .Handle(new CreateRoomTypeCommand(" Deluxe ", "  Desc  "), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.RoomTypes.AlreadyExists.Code);
        }
    }

    public class DeleteRoomTypeCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        [Fact]
        public async Task Handle_Success_SoftDeletes()
        {
            var rtId = Guid.NewGuid();
            var rt = TestHelpers.CreateRoomType(id: rtId);
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType> { rt }.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.HotelRoomTypes).Returns(new List<HotelRoomType>().AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await new DeleteRoomTypeCommandHandler(_db.Object)
                .Handle(new DeleteRoomTypeCommand(rtId), default);

            result.IsError.Should().BeFalse();
            rt.DeletedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_NotFound_ReturnsError()
        {
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType>().AsQueryable().BuildMockDbSet().Object);

            var result = await new DeleteRoomTypeCommandHandler(_db.Object)
                .Handle(new DeleteRoomTypeCommand(Guid.NewGuid()), default);

            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_HasAssociatedHotelRoomTypes_ReturnsConflict()
        {
            var rtId = Guid.NewGuid();
            var rt = TestHelpers.CreateRoomType(id: rtId);
            var hrt = TestHelpers.CreateHotelRoomType(roomTypeId: rtId);
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType> { rt }.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.HotelRoomTypes).Returns(new List<HotelRoomType> { hrt }.AsQueryable().BuildMockDbSet().Object);

            var result = await new DeleteRoomTypeCommandHandler(_db.Object)
                .Handle(new DeleteRoomTypeCommand(rtId), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.RoomTypes.HasRelatedHotelAssignments.Code);
        }
    }

    public class UpdateRoomTypeCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        [Fact]
        public async Task Handle_Success_Updates()
        {
            var rtId = Guid.NewGuid();
            var rt = TestHelpers.CreateRoomType(id: rtId, name: "Old");
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType> { rt }.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await new UpdateRoomTypeCommandHandler(_db.Object)
                .Handle(new UpdateRoomTypeCommand(rtId, "New Name", null), default);

            result.IsError.Should().BeFalse();
            result.Value.Name.Should().Be("New Name");
        }

        [Fact]
        public async Task Handle_NotFound_ReturnsError()
        {
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType>().AsQueryable().BuildMockDbSet().Object);

            var result = await new UpdateRoomTypeCommandHandler(_db.Object)
                .Handle(new UpdateRoomTypeCommand(Guid.NewGuid(), "X", null), default);

            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_DuplicateName_ReturnsConflict()
        {
            var rtId = Guid.NewGuid();
            var rt = TestHelpers.CreateRoomType(id: rtId, name: "Suite");
            var other = TestHelpers.CreateRoomType(name: "Deluxe");
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType> { rt, other }.AsQueryable().BuildMockDbSet().Object);

            var result = await new UpdateRoomTypeCommandHandler(_db.Object)
                .Handle(new UpdateRoomTypeCommand(rtId, "Deluxe", null), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.RoomTypes.AlreadyExists.Code);
        }

        [Fact]
        public async Task Handle_DbUniqueViolation_ReturnsAlreadyExists()
        {
            var rtId = Guid.NewGuid();
            var rt = TestHelpers.CreateRoomType(id: rtId, name: "Suite");
            _db.Setup(x => x.RoomTypes).Returns(new List<RoomType> { rt }.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("room_types unique", new Exception("IX_room_types_Name")));

            var result = await new UpdateRoomTypeCommandHandler(_db.Object)
                .Handle(new UpdateRoomTypeCommand(rtId, "Deluxe", null), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.RoomTypes.AlreadyExists.Code);
        }
    }

    public class GetRoomTypesQueryHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        [Fact]
        public async Task Handle_ReturnsPaginated()
        {
            var types = new List<RoomType>
        {
            TestHelpers.CreateRoomType(name: "Deluxe"),
            TestHelpers.CreateRoomType(name: "Suite"),
        };
            _db.Setup(x => x.RoomTypes).Returns(types.AsQueryable().BuildMockDbSet().Object);

            var result = await new GetRoomTypesQueryHandler(_db.Object)
                .Handle(new GetRoomTypesQuery(null, 1, 10), default);

            result.IsError.Should().BeFalse();
            result.Value.Items.Count.Should().Be(2);
        }

        [Fact]
        public async Task Handle_SearchFilter_Works()
        {
            var types = new List<RoomType>
        {
            TestHelpers.CreateRoomType(name: "Deluxe"),
            TestHelpers.CreateRoomType(name: "Suite"),
        };
            _db.Setup(x => x.RoomTypes).Returns(types.AsQueryable().BuildMockDbSet().Object);

            var result = await new GetRoomTypesQueryHandler(_db.Object)
                .Handle(new GetRoomTypesQuery("Deluxe", 1, 10), default);

            result.IsError.Should().BeFalse();
            result.Value.Items.Should().OnlyContain(x => x.Name.Contains("Deluxe"));
        }
    }

}
namespace HotelBooking.Application.Tests.Admin.Services
{

    public class CreateServiceCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        [Fact]
        public async Task Handle_Success_CreatesService()
        {
            _db.Setup(x => x.Services).Returns(new List<Service>().AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await new CreateServiceCommandHandler(_db.Object)
                .Handle(new CreateServiceCommand("WiFi", "High speed"), default);

            result.IsError.Should().BeFalse();
            result.Value.Name.Should().Be("WiFi");
        }

        [Fact]
        public async Task Handle_DuplicateName_ReturnsAlreadyExists()
        {
            var existing = TestHelpers.CreateService(name: "WiFi");
            _db.Setup(x => x.Services).Returns(new List<Service> { existing }.AsQueryable().BuildMockDbSet().Object);

            var result = await new CreateServiceCommandHandler(_db.Object)
                .Handle(new CreateServiceCommand("WiFi", null), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.Services.AlreadyExists.Code);
        }

        [Fact]
        public async Task Handle_DbUniqueViolation_ReturnsAlreadyExists()
        {
            _db.Setup(x => x.Services).Returns(new List<Service>().AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("services unique", new Exception("IX_services_Name")));

            var result = await new CreateServiceCommandHandler(_db.Object)
                .Handle(new CreateServiceCommand(" WiFi ", "  Fast  "), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.Services.AlreadyExists.Code);
        }
    }

    public class DeleteServiceCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        [Fact]
        public async Task Handle_Success_SoftDeletes()
        {
            var svcId = Guid.NewGuid();
            var svc = TestHelpers.CreateService(id: svcId);
            _db.Setup(x => x.Services).Returns(new List<Service> { svc }.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.HotelServices).Returns(new List<HotelService>().AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await new DeleteServiceCommandHandler(_db.Object)
                .Handle(new DeleteServiceCommand(svcId), default);

            result.IsError.Should().BeFalse();
            svc.DeletedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_NotFound_ReturnsError()
        {
            _db.Setup(x => x.Services).Returns(new List<Service>().AsQueryable().BuildMockDbSet().Object);

            var result = await new DeleteServiceCommandHandler(_db.Object)
                .Handle(new DeleteServiceCommand(Guid.NewGuid()), default);

            result.IsError.Should().BeTrue();
        }
    }

    public class UpdateServiceCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        [Fact]
        public async Task Handle_Success_UpdatesService()
        {
            var svcId = Guid.NewGuid();
            var svc = TestHelpers.CreateService(id: svcId, name: "Old Name");
            _db.Setup(x => x.Services).Returns(new List<Service> { svc }.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await new UpdateServiceCommandHandler(_db.Object)
                .Handle(new UpdateServiceCommand(svcId, "New Name", null), default);

            result.IsError.Should().BeFalse();
            result.Value.Name.Should().Be("New Name");
        }

        [Fact]
        public async Task Handle_NotFound_ReturnsError()
        {
            _db.Setup(x => x.Services).Returns(new List<Service>().AsQueryable().BuildMockDbSet().Object);

            var result = await new UpdateServiceCommandHandler(_db.Object)
                .Handle(new UpdateServiceCommand(Guid.NewGuid(), "X", null), default);

            result.IsError.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_DuplicateName_ReturnsAlreadyExists()
        {
            var svcId = Guid.NewGuid();
            var svc = TestHelpers.CreateService(id: svcId, name: "Old Name");
            var other = TestHelpers.CreateService(name: "WiFi");
            _db.Setup(x => x.Services).Returns(new List<Service> { svc, other }.AsQueryable().BuildMockDbSet().Object);

            var result = await new UpdateServiceCommandHandler(_db.Object)
                .Handle(new UpdateServiceCommand(svcId, "WiFi", null), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.Services.AlreadyExists.Code);
        }

        [Fact]
        public async Task Handle_DbUniqueViolation_ReturnsAlreadyExists()
        {
            var svcId = Guid.NewGuid();
            var svc = TestHelpers.CreateService(id: svcId, name: "Old Name");
            _db.Setup(x => x.Services).Returns(new List<Service> { svc }.AsQueryable().BuildMockDbSet().Object);
            _db.Setup(x => x.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("services unique", new Exception("IX_services_Name")));

            var result = await new UpdateServiceCommandHandler(_db.Object)
                .Handle(new UpdateServiceCommand(svcId, "WiFi", " Fast "), default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(AdminErrors.Services.AlreadyExists.Code);
        }
    }
}
