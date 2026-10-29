using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Diaries;

public sealed class ServingSizeSnapshot : ValueObject
{
    internal ServingSizeSnapshot(ServingSize servingSize) : this(
        servingSize.Id,
        servingSize.Quantity,
        servingSize.Unit,
        servingSize.Label,
        servingSize.EquivalentQuantity
    )
    {
    }

    private ServingSizeSnapshot(
        ServingSizeId servingSizeId,
        decimal quantity,
        string unit,
        string? label,
        decimal equivalentQuantity
    )
    {
        ServingSizeId = servingSizeId;
        Quantity = quantity;
        Unit = unit;
        Label = label;
        EquivalentQuantity = equivalentQuantity;
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
