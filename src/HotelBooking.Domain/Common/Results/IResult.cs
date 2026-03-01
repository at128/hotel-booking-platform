namespace HotelBooking.Domain.Common.Results;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsError { get { return !IsSuccess; } }
    List<Error> Errors { get; }
}

public interface IResult<out TValue> : IResult
{
    TValue Value { get; }
}