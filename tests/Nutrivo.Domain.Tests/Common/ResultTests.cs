using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ContainsValueAndNoError()
    {
        var result = Result<string>.Success("Food");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Food", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_CanContainDefaultValueOfValueType()
    {
        var result = Result<int>.Success(0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ContainsErrorAndDoesNotExposeAnInvalidValue()
    {
        var error = new DomainError("Example.Invalid", "The input is invalid.");

        var result = Result<string>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_ForValueType_DoesNotExposeDefaultValueAsSuccess()
    {
        var result = Result<int>.Failure(
            new DomainError("Example.Invalid", "Invalid input.")
        );

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Success_WithNullValue_RejectsProgrammingError()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    [Fact]
    public void Failure_WithNullError_RejectsProgrammingError()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }
}
