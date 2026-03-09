/// <summary>
/// Tests for Cart command/query handlers and MediatR pipeline behaviors.
/// </summary>
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HotelBooking.Application.Common.Behaviors;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Features.Cart.Commands.AddToCart;
using HotelBooking.Application.Tests._Shared;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Cart;
using HotelBooking.Domain.Common.Results;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace HotelBooking.Application.Tests.Cart
{
    public class AddToCartCommandHandlerTests
    {
        private readonly Mock<IAppDbContext> _db = new();

        private AddToCartCommandHandler CreateHandler() => new(_db.Object);

        private void SetupHotelRoomTypes(List<HotelRoomType> items)
        {
            var mock = items.AsQueryable().BuildMockDbSet();
            _db.Setup(x => x.HotelRoomTypes).Returns(mock.Object);
        }

        private void SetupCartItems(List<CartItem> items)
        {
            var mock = items.AsQueryable().BuildMockDbSet();
            _db.Setup(x => x.CartItems).Returns(mock.Object);
            _db.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        }

        private void SetupRooms(List<Room> items)
        {
            var mock = items.AsQueryable().BuildMockDbSet();
            _db.Setup(x => x.Rooms).Returns(mock.Object);
        }

        private void SetupBookingRooms(List<BookingRoom> items)
        {
            var mock = items.AsQueryable().BuildMockDbSet();
            _db.Setup(x => x.BookingRooms).Returns(mock.Object);
        }

        private void SetupCheckoutHolds(List<CheckoutHold> items)
        {
            var mock = items.AsQueryable().BuildMockDbSet();
            _db.Setup(x => x.CheckoutHolds).Returns(mock.Object);
        }

        [Fact]
        public async Task Handle_NewItem_ReturnsCartItemDto()
        {
            // Arrange
            var hrt = TestHelpers.CreateHotelRoomType();
            SetupHotelRoomTypes([hrt]);
            SetupCartItems([]);

            SetupRooms([
                TestHelpers.CreateRoom(hotelRoomTypeId: hrt.Id)
            ]);

            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: Guid.NewGuid(),
                HotelRoomTypeId: hrt.Id,
                CheckIn: new DateOnly(2026, 8, 1),
                CheckOut: new DateOnly(2026, 8, 5),
                Quantity: 1,
                Adults: 2,
                Children: 0);

            // Act
            var result = await CreateHandler().Handle(cmd, default);

            // Assert
            result.IsError.Should().BeFalse();
            result.Value.HotelRoomTypeId.Should().Be(hrt.Id);
            result.Value.Quantity.Should().Be(1);
            result.Value.Adults.Should().Be(2);
            result.Value.Children.Should().Be(0);
        }

        [Fact]
        public async Task Handle_RoomTypeNotFound_ReturnsError()
        {
            // Arrange
            SetupHotelRoomTypes([]);
            SetupCartItems([]);
            SetupRooms([]);
            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: Guid.NewGuid(),
                HotelRoomTypeId: Guid.NewGuid(),
                CheckIn: new DateOnly(2026, 8, 1),
                CheckOut: new DateOnly(2026, 8, 5),
                Quantity: 1,
                Adults: 2,
                Children: 0);

            // Act
            var result = await CreateHandler().Handle(cmd, default);

            // Assert
            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(ApplicationErrors.Cart.RoomTypeNotFound.Code);
        }

        [Fact]
        public async Task Handle_InvalidDates_CheckOutBeforeCheckIn_ReturnsError()
        {
            // Arrange
            var hrt = TestHelpers.CreateHotelRoomType();
            SetupHotelRoomTypes([hrt]);
            SetupCartItems([]);
            SetupRooms([
                TestHelpers.CreateRoom(hotelRoomTypeId: hrt.Id)
            ]);
            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: Guid.NewGuid(),
                HotelRoomTypeId: hrt.Id,
                CheckIn: new DateOnly(2026, 8, 5),
                CheckOut: new DateOnly(2026, 8, 1),
                Quantity: 1,
                Adults: 2,
                Children: 0);

            // Act
            var result = await CreateHandler().Handle(cmd, default);

            // Assert
            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(ApplicationErrors.Cart.InvalidDates.Code);
        }

        [Fact]
        public async Task Handle_HotelMismatch_DifferentHotelInCart_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var hotelIdA = Guid.NewGuid();
            var hotelIdB = Guid.NewGuid();

            var hrtB = TestHelpers.CreateHotelRoomType(hotelId: hotelIdB);
            SetupHotelRoomTypes([hrtB]);

            var existing = TestHelpers.CreateCartItem(
                userId: userId,
                hotelId: hotelIdA,
                checkIn: new DateOnly(2026, 8, 1),
                checkOut: new DateOnly(2026, 8, 5),
                adults: 2,
                children: 0);

            SetupCartItems([existing]);
            SetupRooms([
                TestHelpers.CreateRoom(hotelRoomTypeId: hrtB.Id)
            ]);
            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: userId,
                HotelRoomTypeId: hrtB.Id,
                CheckIn: new DateOnly(2026, 8, 1),
                CheckOut: new DateOnly(2026, 8, 5),
                Quantity: 1,
                Adults: 2,
                Children: 0);

            // Act
            var result = await CreateHandler().Handle(cmd, default);

            // Assert
            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(ApplicationErrors.Cart.HotelMismatch.Code);
        }

        [Fact]
        public async Task Handle_DateMismatch_DifferentDatesInCart_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var hrt = TestHelpers.CreateHotelRoomType(hotelId: hotelId);
            SetupHotelRoomTypes([hrt]);

            var existing = TestHelpers.CreateCartItem(
                userId: userId,
                hotelId: hotelId,
                hotelRoomTypeId: Guid.NewGuid(),
                checkIn: new DateOnly(2026, 9, 1),
                checkOut: new DateOnly(2026, 9, 5),
                adults: 2,
                children: 0);

            SetupCartItems([existing]);
            SetupRooms([
                TestHelpers.CreateRoom(hotelRoomTypeId: hrt.Id)
            ]);
            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: userId,
                HotelRoomTypeId: hrt.Id,
                CheckIn: new DateOnly(2026, 8, 1),
                CheckOut: new DateOnly(2026, 8, 5),
                Quantity: 1,
                Adults: 2,
                Children: 0);

            // Act
            var result = await CreateHandler().Handle(cmd, default);

            // Assert
            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(ApplicationErrors.Cart.DateMismatch.Code);
        }

        [Fact]
        public async Task Handle_RoomOccupancyExceeded_ReturnsError()
        {
            // Arrange
            var hrt = TestHelpers.CreateHotelRoomType();
            SetupHotelRoomTypes([hrt]);
            SetupCartItems([]);
            SetupRooms([
                TestHelpers.CreateRoom(hotelRoomTypeId: hrt.Id)
            ]);
            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: Guid.NewGuid(),
                HotelRoomTypeId: hrt.Id,
                CheckIn: new DateOnly(2026, 8, 1),
                CheckOut: new DateOnly(2026, 8, 5),
                Quantity: 1,
                Adults: 10,
                Children: 10);

            // Act
            var result = await CreateHandler().Handle(cmd, default);

            // Assert
            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(ApplicationErrors.Cart.RoomOccupancyExceeded.Code);
        }

        [Fact]
        public async Task Handle_QuantityExceedsCapacity_ReturnsError()
        {
            // Arrange
            var hrt = TestHelpers.CreateHotelRoomType();
            SetupHotelRoomTypes([hrt]);
            SetupCartItems([]);

            // no available rooms
            SetupRooms([]);
            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: Guid.NewGuid(),
                HotelRoomTypeId: hrt.Id,
                CheckIn: new DateOnly(2026, 8, 1),
                CheckOut: new DateOnly(2026, 8, 5),
                Quantity: 1,
                Adults: 2,
                Children: 0);

            // Act
            var result = await CreateHandler().Handle(cmd, default);

            // Assert
            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(ApplicationErrors.Cart.QuantityExceedsCapacity.Code);
        }

        [Fact]
        public async Task Handle_InvalidGuests_ReturnsError()
        {
            var hrt = TestHelpers.CreateHotelRoomType();
            SetupHotelRoomTypes([hrt]);
            SetupCartItems([]);
            SetupRooms([]);
            SetupBookingRooms([]);
            SetupCheckoutHolds([]);

            var cmd = new AddToCartCommand(
                UserId: Guid.NewGuid(),
                HotelRoomTypeId: hrt.Id,
                CheckIn: new DateOnly(2026, 8, 1),
                CheckOut: new DateOnly(2026, 8, 5),
                Quantity: 1,
                Adults: 0,
                Children: -1);

            var result = await CreateHandler().Handle(cmd, default);

            result.IsError.Should().BeTrue();
            result.TopError.Code.Should().Be(ApplicationErrors.Cart.InvalidGuests.Code);
        }

        [Theory]
        [InlineData("duplicate key value", true)]
        [InlineData("UNIQUE KEY violation", true)]
        [InlineData("Some unique constraint violation", true)]
        [InlineData("Foreign key violation", false)]
        public void IsLikelyUniqueConstraintViolation_DetectsPatterns(string message, bool expected)
        {
            var method = typeof(AddToCartCommandHandler)
                .GetMethod("IsLikelyUniqueConstraintViolation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            var ex = new DbUpdateException("update failed", new Exception(message));
            var actual = (bool)method.Invoke(null, [ex])!;

            actual.Should().Be(expected);
        }
    }
}

namespace HotelBooking.Application.Tests.Behaviors
{
    public sealed class TestResult : IResult, IValidationFailureFactory<TestResult>
    {
        public bool IsSuccess { get; private init; }
        public bool IsError => !IsSuccess;
        public List<Error> Errors { get; private init; } = [];

        public static TestResult Success() => new() { IsSuccess = true };

        public static TestResult FromValidationErrors(IReadOnlyCollection<Error> errors) =>
            new() { IsSuccess = false, Errors = errors.ToList() };
    }

    public sealed record TestRequest : IRequest<TestResult>;

    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_NoValidators_CallsNext()
        {
            var behavior = new ValidationBehavior<TestRequest, TestResult>([]);
            var nextCalled = false;

            Task<TestResult> Next(CancellationToken _)
            {
                nextCalled = true;
                return Task.FromResult(TestResult.Success());
            }

            var result = await behavior.Handle(new TestRequest(), Next, default);

            nextCalled.Should().BeTrue();
            result.IsError.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsNext()
        {
            var mockValidator = new Mock<IValidator<TestRequest>>();
            mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), default))
                .ReturnsAsync(new ValidationResult());

            var behavior = new ValidationBehavior<TestRequest, TestResult>([mockValidator.Object]);
            var nextCalled = false;

            Task<TestResult> Next(CancellationToken _)
            {
                nextCalled = true;
                return Task.FromResult(TestResult.Success());
            }

            await behavior.Handle(new TestRequest(), Next, default);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_InvalidRequest_ReturnsValidationErrors()
        {
            var failures = new List<ValidationFailure>
            {
                new("Name", "Name is required"),
            };

            var mockValidator = new Mock<IValidator<TestRequest>>();
            mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), default))
                .ReturnsAsync(new ValidationResult(failures));

            var behavior = new ValidationBehavior<TestRequest, TestResult>([mockValidator.Object]);

            var result = await behavior.Handle(
                new TestRequest(),
                _ => Task.FromResult(TestResult.Success()),
                default);

            result.IsError.Should().BeTrue();
            result.Errors.Should().HaveCount(1);
        }
    }

    public class PerformanceBehaviorTests
    {
        [Fact]
        public async Task Handle_FastRequest_DoesNotLogWarning()
        {
            var mockLogger = new Mock<ILogger<PerformanceBehavior<TestRequest, TestResult>>>();
            var behavior = new PerformanceBehavior<TestRequest, TestResult>(mockLogger.Object);

            await behavior.Handle(
                new TestRequest(),
                _ => Task.FromResult(TestResult.Success()),
                default);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_SlowRequest_LogsWarning()
        {
            var mockLogger = new Mock<ILogger<PerformanceBehavior<TestRequest, TestResult>>>();
            var behavior = new PerformanceBehavior<TestRequest, TestResult>(mockLogger.Object);

            await behavior.Handle(
                new TestRequest(),
                async _ =>
                {
                    await Task.Delay(600);
                    return TestResult.Success();
                },
                default);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    public class UnhandledExceptionBehaviorTests
    {
        [Fact]
        public async Task Handle_NoException_ReturnsResult()
        {
            var mockLogger = new Mock<ILogger<UnhandledExceptionBehavior<TestRequest, TestResult>>>();
            var behavior = new UnhandledExceptionBehavior<TestRequest, TestResult>(mockLogger.Object);

            var result = await behavior.Handle(
                new TestRequest(),
                _ => Task.FromResult(TestResult.Success()),
                default);

            result.IsError.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ExceptionThrown_LogsAndRethrows()
        {
            var mockLogger = new Mock<ILogger<UnhandledExceptionBehavior<TestRequest, TestResult>>>();
            var behavior = new UnhandledExceptionBehavior<TestRequest, TestResult>(mockLogger.Object);

            var act = () => behavior.Handle(
                new TestRequest(),
                _ => throw new InvalidOperationException("Boom!"),
                default);

            await act.Should().ThrowAsync<InvalidOperationException>();

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
