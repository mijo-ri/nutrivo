using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Diaries;

public sealed class FoodSnapshot : ValueObject
{
    internal FoodSnapshot(Food food)
    {
        FoodId = food.Id;
        Name = food.Name;
        Brand = food.Brand;
        ReferenceQuantity = food.ReferenceQuantity;
        ReferenceUnit = food.ReferenceUnit;
        NutritionalValues = food.NutritionalValues;
    }

    public FoodId FoodId { get; }
    public string Name { get; }
    public string? Brand { get; }
    public decimal ReferenceQuantity { get; }
    public ReferenceUnit ReferenceUnit { get; }
    public NutritionalValues NutritionalValues { get; }

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
