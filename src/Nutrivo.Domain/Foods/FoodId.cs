using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Foods;

public sealed class FoodId : ValueObject
{
    private FoodId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static FoodId New()
    {
        return new FoodId(Guid.NewGuid());
    }

    public static Result<FoodId> Create(Guid value)
    {
        return value == Guid.Empty
            ? Result<FoodId>.Failure(FoodErrors.EmptyId)
            : Result<FoodId>.Success(new FoodId(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
