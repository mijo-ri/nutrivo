using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Diaries;

public sealed class FoodSnapshot : ValueObject
{
    internal FoodSnapshot(Food food) : this(
        food.Id,
        food.Name,
        food.Brand,
        food.ReferenceQuantity,
        food.ReferenceUnit
    )
    {
        NutritionalValues = NutritionalValues.Create(
                food.NutritionalValues.Kilocalories,
                food.NutritionalValues.ProteinGrams,
                food.NutritionalValues.CarbohydratesGrams,
                food.NutritionalValues.FatGrams
            )
            .Value;
    }

    // Persistence supplies the nutritional values after scalar binding.
    private FoodSnapshot(
        FoodId foodId,
        string name,
        string? brand,
        decimal referenceQuantity,
        ReferenceUnit referenceUnit
    )
    {
        FoodId = foodId;
        Name = name;
        Brand = brand;
        ReferenceQuantity = referenceQuantity;
        ReferenceUnit = referenceUnit;
    }

    public FoodId FoodId { get; }
    public string Name { get; }
    public string? Brand { get; }
    public decimal ReferenceQuantity { get; }
    public ReferenceUnit ReferenceUnit { get; }
    public NutritionalValues NutritionalValues { get; } = null!;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FoodId;
        yield return Name;
        yield return Brand;
        yield return ReferenceQuantity;
        yield return ReferenceUnit;
        yield return NutritionalValues;
    }
}
