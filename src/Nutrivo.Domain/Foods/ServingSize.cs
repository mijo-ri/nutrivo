using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Foods;

public sealed class ServingSize : Entity<ServingSizeId>
{
    private ServingSize(
        ServingSizeId id,
        decimal quantity,
        string unit,
        string? label,
        decimal equivalentQuantity,
        int position
    ) : base(id)
    {
        Quantity = quantity;
        Unit = unit;
        Label = label;
        EquivalentQuantity = equivalentQuantity;
        Position = position;
    }

    public int Position { get; }
    public decimal Quantity { get; }
    public string Unit { get; }
    public string? Label { get; }
    public decimal EquivalentQuantity { get; }

    internal ServingSize AtPosition(int position)
    {
        return new ServingSize(Id, Quantity, Unit, Label, EquivalentQuantity, position);
    }

    public static Result<ServingSize> Create(
        decimal quantity,
        string? unit,
        string? label,
        decimal equivalentQuantity
    )
    {
        if (quantity <= 0)
        {
            return Result<ServingSize>.Failure(FoodErrors.InvalidServingQuantity);
        }

        var trimmedUnit = unit?.Trim();
        if (string.IsNullOrEmpty(trimmedUnit))
        {
            return Result<ServingSize>.Failure(FoodErrors.ServingUnitRequired);
        }

        if (trimmedUnit.Length > 200)
        {
            return Result<ServingSize>.Failure(FoodErrors.ServingUnitTooLong);
        }

        var trimmedLabel = label?.Trim();
        if (trimmedLabel?.Length > 200)
        {
            return Result<ServingSize>.Failure(FoodErrors.ServingLabelTooLong);
        }

        if (equivalentQuantity <= 0)
        {
            return Result<ServingSize>.Failure(FoodErrors.InvalidEquivalentQuantity);
        }

        return Result<ServingSize>.Success(
            new ServingSize(
                ServingSizeId.New(),
                quantity,
                trimmedUnit,
                string.IsNullOrEmpty(trimmedLabel) ? null : trimmedLabel,
                equivalentQuantity,
                0
            )
        );
    }
}
