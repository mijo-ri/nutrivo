using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Foods;

public sealed class ServingSizeId : ValueObject
{
    private ServingSizeId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static ServingSizeId New()
    {
        return new ServingSizeId(Guid.NewGuid());
    }

    public static Result<ServingSizeId> Create(Guid value)
    {
        return value == Guid.Empty
            ? Result<ServingSizeId>.Failure(FoodErrors.EmptyServingSizeId)
            : Result<ServingSizeId>.Success(new ServingSizeId(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
