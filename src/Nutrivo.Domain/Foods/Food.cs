using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Foods;

public sealed class Food : AggregateRoot<FoodId>
{
    private const int MaximumNameLength = 200;
    private const int MaximumBrandLength = 200;

    private Food(
        FoodId id,
        string name,
        string? brand,
        ReferenceUnit referenceUnit,
        NutritionalValues nutritionalValues
    ) : base(id)
    {
        Name = name;
        Brand = brand;
        ReferenceUnit = referenceUnit;
        NutritionalValues = nutritionalValues;
    }

    public string Name { get; }

    public string? Brand { get; }

    public decimal ReferenceQuantity
    {
        get
        {
            return 100m;
        }
    }

    public ReferenceUnit ReferenceUnit { get; }

    public NutritionalValues NutritionalValues { get; }

    public static Result<Food> Create(
        string? name,
        string? brand,
        ReferenceUnit referenceUnit,
        NutritionalValues? nutritionalValues
    )
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            return Result<Food>.Failure(FoodErrors.NameRequired);
        }

        if (trimmedName.Length > MaximumNameLength)
        {
            return Result<Food>.Failure(FoodErrors.NameTooLong);
        }

        var trimmedBrand = brand?.Trim();

        if (trimmedBrand?.Length > MaximumBrandLength)
        {
            return Result<Food>.Failure(FoodErrors.BrandTooLong);
        }

        if (referenceUnit is not (ReferenceUnit.Gram or ReferenceUnit.Milliliter))
        {
            return Result<Food>.Failure(FoodErrors.InvalidReferenceUnit);
        }

        if (nutritionalValues is null)
        {
            return Result<Food>.Failure(FoodErrors.NutritionalValuesRequired);
        }

        return Result<Food>.Success(
            new Food(
                FoodId.New(),
                trimmedName,
                string.IsNullOrEmpty(trimmedBrand) ? null : trimmedBrand,
                referenceUnit,
                nutritionalValues
            )
        );
    }
}
