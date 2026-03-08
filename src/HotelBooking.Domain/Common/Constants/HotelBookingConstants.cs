namespace HotelBooking.Domain.Common.Constants;

public static class HotelBookingConstants
{

    public const string Currency = "USD";
    public const string Timezone = "UTC";


    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }


    public static class Booking
    {
        public const int MinimumStayNights = 1;
        public const decimal MvpDiscountAmount = 0.00m;
    }


    public static class Cart
    {
        public const int MinQuantity = 1;
        public const int MaxQuantity = 10;
    }


    public static class Hotel
    {
        public const int MinStarRating = 1;
        public const int MaxStarRating = 5;
        public const string DefaultCheckInTime = "14:00";
        public const string DefaultCheckOutTime = "11:00";
    }


    public static class Review
    {
        public const int MinRating = 1;
        public const int MaxRating = 5;
        public const int TitleMaxLength = 100;
        public const int CommentMaxLength = 1000;
    }


    public static class Image
    {
        public static readonly string[] AllowedExtensions =
            [".jpg", ".jpeg", ".png", ".webp"];

        public static readonly string[] AllowedMimeTypes =
            ["image/jpeg", "image/png", "image/webp"];

        public const int CaptionMaxLength = 200;
        public const string StoragePathTemplate = "/images/{0}/{1}";
    }


    public static class Search
    {
        public const int DefaultAdults = 2;
        public const int DefaultChildren = 0;
        public const int DefaultRooms = 1;
        public const string DefaultSortBy = "price_asc";
    }

    public static class Pagination
    {
        public const int SearchDefaultLimit = 20;
        public const int CursorMaxLimit = 50;
        public const int ReviewsDefaultLimit = 10;
        public const int BookingsDefaultLimit = 10;
        public const int AdminDefaultPageSize = 20;
    }


    public static class FieldLengths
    {
        // Auth / User
        public const int EmailMaxLength = 100;
        public const int PasswordMinLength = 8;
        public const int FirstNameMaxLength = 100;
        public const int LastNameMaxLength = 100;
        public const int PhoneNumberMaxLength = 20;

        // City
        public const int CityNameMaxLength = 100;
        public const int CountryMaxLength = 100;
        public const int PostOfficeMaxLength = 20;

        // Hotel
        public const int HotelNameMaxLength = 200;
        public const int HotelDescriptionMaxLength = 2000;
        public const int HotelAddressMaxLength = 500;
        public const int HotelOwnerMaxLength = 200;
        public const int CheckInOutTimeMaxLength = 5;

        // Room
        public const int RoomNumberMaxLength = 10;
        public const int RoomTypeNameMaxLength = 100;
        public const int RoomTypeDescriptionMaxLength = 500;

        // General
        public const int UrlMaxLength = 500;
    }
}