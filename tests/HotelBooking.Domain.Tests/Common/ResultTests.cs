/// <summary>
/// Tests for Result&lt;T&gt; — implicit conversions, IsError, Value, TopError, and Match.
/// </summary>
using FluentAssertions;
using HotelBooking.Domain.Common.Results;
using Xunit;
namespace HotelBooking.Domain.Tests.Common;

public class ResultTests
{
    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesErrorResult()
    {
        var error = Error.NotFound("Test.NotFound", "Not found.");

        Result<string> result = error;

        result.IsError.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region IsError / IsSuccess

    [Fact]
    public void IsError_WhenSuccess_ReturnsFalse()
    {
        Result<int> result = 42;

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void IsError_WhenError_ReturnsTrue()
    {
        Result<int> result = Error.Failure("F", "Failure");

        result.IsError.Should().BeTrue();
    }

    #endregion

    #region Value

    [Fact]
    public void Value_WhenSuccess_ReturnsValue()
    {
        Result<int> result = 100;

        result.Value.Should().Be(100);
    }

    #endregion

    #region TopError

    [Fact]
    public void TopError_WhenError_ReturnsFirstError()
    {
        var error = Error.NotFound("Code.1", "Not found");

        Result<string> result = error;

        result.TopError.Code.Should().Be("Code.1");
        result.TopError.Description.Should().Be("Not found");
        result.TopError.Type.Should().Be(ErrorKind.NotFound);
    }

    #endregion

    #region Match

    [Fact]
    public void Match_WhenSuccess_CallsOnValue()
    {
        Result<int> result = 5;

        var output = result.Match(
            onValue: v => $"value:{v}",
            onError: _ => "error");

        output.Should().Be("value:5");
    }

    [Fact]
    public void Match_WhenError_CallsOnError()
    {
        Result<int> result = Error.Failure("X", "desc");

        var output = result.Match(
            onValue: _ => "value",
            onError: errors => $"errors:{errors.Count}");

        output.Should().Be("errors:1");
    }

    #endregion
}
