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

    public int Position { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; }
    public string? Label { get; private set; }
    public decimal EquivalentQuantity { get; private set; }

    internal void Apply(ServingSize validated)
    {
        Quantity = validated.Quantity;
        Unit = validated.Unit;
        Label = validated.Label;
        EquivalentQuantity = validated.EquivalentQuantity;
        Position = validated.Position;
    }

    internal ServingSize AtPosition(int position)
    {
        return new ServingSize(Id, Quantity, Unit, Label, EquivalentQuantity, position);
    }

    public static Result<ServingSize> Create(
        decimal quantity,
        string? unit,
        string? label,
        decimal equivalentQuantity,
        ServingSizeId? id = null
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
                id ?? ServingSizeId.New(),
                quantity,
                trimmedUnit,
                string.IsNullOrEmpty(trimmedLabel) ? null : trimmedLabel,
                equivalentQuantity,
                0
            )
        );
    }
}
