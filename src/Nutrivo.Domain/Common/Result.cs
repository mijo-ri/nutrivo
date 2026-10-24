namespace Nutrivo.Domain.Common;

public sealed class Result<T>
    where T : notnull
{
    private readonly T? _value;

    private Result(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        IsSuccess = true;
    }

    private Result(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure
    {
        get
        {
            return !IsSuccess;
        }
    }

    public DomainError? Error { get; }

    public T Value
    {
        get
        {
            return IsSuccess
                ? _value!
                : throw new InvalidOperationException("A failed result has no value.");
        }
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public static Result<T> Failure(DomainError error)
    {
        return new Result<T>(error);
    }
}
