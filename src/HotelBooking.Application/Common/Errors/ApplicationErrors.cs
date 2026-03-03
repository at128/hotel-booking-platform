
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Contracts.Auth;
using HotelBooking.Domain.Common.Results;

namespace HotelBooking.Application.Common.Errors;
public static class ApplicationErrors
{
    public static class Auth
    {
        public static readonly Error EmailAlreadyRegistered =
            Error.Conflict("Auth.EmailAlreadyRegistered", "Email is already registered.");

        public static Error RegistrationFailed(string details) =>
            Error.Failure("Auth.RegistrationFailed", $"Registration failed: {details}");

        public static readonly Error InvalidCredentials =
            Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");

        public static readonly Error UserNotFound =
            Error.NotFound("Auth.UserNotFound", "User not found.");

        public static readonly Error UpdateFailed =
            Error.Failure("Auth.UpdateFailed", "Profile update failed.");

        public static readonly Error AccountLocked =
            Error.Failure("Auth.AccountLocked", "Account is temporarily locked. Please try again later");

        public static readonly Error InvalidRefreshToken =
            Error.Validation("Auth.InvalidRefreshToken", "Refresh token is invalid.");

        public static readonly Error RefreshTokenReuse =
            Error.Failure("Auth.RefreshTokenReuse", "Refresh token reuse detected. Please login again.");
    }
    public static class Cart
    {
        public static readonly Error RoomTypeNotFound =
            Error.NotFound("Cart.RoomTypeNotFound", "Room type not found.");

        public static readonly Error HotelMismatch =
            Error.Conflict("Cart.HotelMismatch",
                "Your cart contains rooms from a different hotel. Clear your cart first.");

        public static readonly Error DateMismatch =
            Error.Conflict("Cart.DateMismatch",
                "All cart items must have the same check-in and check-out dates.");

        public static readonly Error InvalidDates =
            Error.Validation("Cart.InvalidDates",
                "Check-out must be after check-in, and check-in must be in the future.");

        public static readonly Error CartItemNotFound =
            Error.NotFound("Cart.ItemNotFound", "Cart item not found.");

        public static readonly Error QuantityExceedsCapacity =
            Error.Conflict("Cart.QuantityExceedsCapacity",
                "Requested quantity exceeds available rooms of this type.");

        public static Error InvalidQuantity(int max) =>
            Error.Validation("Cart.InvalidQuantity",
                $"Quantity must be between 1 and {max}.");
    }

    public static class Checkout
    {
        public static Error RoomUnavailable(string roomTypeName) =>
            Error.Conflict("Checkout.RoomUnavailable",
                $"'{roomTypeName}' is no longer available for the selected dates. Please update your cart.");

        public static readonly Error CartEmpty =
            Error.Validation("Checkout.CartEmpty", "Your cart is empty.");

        public static readonly Error HoldExpired =
            Error.Conflict("Checkout.HoldExpired",
                "Your checkout session has expired. Please try again.");
    }
    public static class Payment
    {
        public static Error RoomNoLongerAvailable(string roomTypeName) =>
            Error.Conflict("Payment.RoomNoLongerAvailable",
                $"'{roomTypeName}' is no longer available. Please update your cart.");

        public static readonly Error GatewayUnavailable =
            Error.Failure("Payment.GatewayUnavailable",
                "Payment service is temporarily unavailable. Please try again shortly.");

        public static readonly Error SessionNotFound =
            Error.NotFound("Payment.SessionNotFound",
                "Payment session not found.");

    }
}