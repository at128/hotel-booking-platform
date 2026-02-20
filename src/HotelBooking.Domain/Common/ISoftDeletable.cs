namespace HotelBooking.Domain.Common;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAtUtc { get; set; }
}