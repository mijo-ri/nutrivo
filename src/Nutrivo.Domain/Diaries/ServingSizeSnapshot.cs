using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Diaries;

public sealed class ServingSizeSnapshot : ValueObject
{
    internal ServingSizeSnapshot(ServingSize servingSize)
    {
        ServingSizeId = servingSize.Id;
        Quantity = servingSize.Quantity;
        Unit = servingSize.Unit;
        Label = servingSize.Label;
        EquivalentQuantity = servingSize.EquivalentQuantity;
    }

    public ServingSizeId ServingSizeId { get; }
    public decimal Quantity { get; }
    public string Unit { get; }
    public string? Label { get; }
    public decimal EquivalentQuantity { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ServingSizeId;
        yield return Quantity;
        yield return Unit;
        yield return Label;
        yield return EquivalentQuantity;
    }
}
