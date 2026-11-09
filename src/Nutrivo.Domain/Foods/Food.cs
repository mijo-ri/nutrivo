using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Foods;

public sealed class Food : AggregateRoot<FoodId>
{
    private const int MaximumNameLength = 200;
    private const int MaximumBrandLength = 200;
    private readonly List<ServingSize> _servingSizes = [];

    private Food(
        FoodId id,
        string name,
        string? brand,
        decimal referenceQuantity,
        ReferenceUnit referenceUnit,
        NutritionalValues nutritionalValues
    ) : this(id, name, brand, referenceQuantity, referenceUnit)
    {
        NutritionalValues = nutritionalValues;
    }

    // Persistence supplies the nutritional values after binding the scalar properties.
    private Food(
        FoodId id,
        string name,
        string? brand,
        decimal referenceQuantity,
        ReferenceUnit referenceUnit
    ) : base(id)
    {
        Name = name;
        Brand = brand;
        ReferenceQuantity = referenceQuantity;
        ReferenceUnit = referenceUnit;
    }

    public string Name { get; private set; }

    public string? Brand { get; private set; }

    public decimal ReferenceQuantity { get; private set; }

    public ReferenceUnit ReferenceUnit { get; private set; }

    public NutritionalValues NutritionalValues { get; private set; } = null!;

    public IReadOnlyList<ServingSize> ServingSizes => _servingSizes
        .OrderBy(serving => serving.Position)
        .ToList()
        .AsReadOnly();

    public static Result<Food> Create(
        string? name,
        string? brand,
        decimal referenceQuantity,
        ReferenceUnit referenceUnit,
        NutritionalValues? nutritionalValues,
        IReadOnlyList<ServingSize?>? servingSizes = null
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

        if (referenceQuantity <= 0)
        {
            return Result<Food>.Failure(FoodErrors.InvalidReferenceQuantity);
        }

        if (nutritionalValues is null)
        {
            return Result<Food>.Failure(FoodErrors.NutritionalValuesRequired);
        }

        var food = new Food(
            FoodId.New(),
            trimmedName,
            string.IsNullOrEmpty(trimmedBrand) ? null : trimmedBrand,
            referenceQuantity,
            referenceUnit,
            nutritionalValues
        );

        foreach (var serving in servingSizes ?? [])
        {
            if (serving is null)
            {
                return Result<Food>.Failure(FoodErrors.ServingSizeRequired);
            }

            if (food._servingSizes.Any(existing => existing.Id == serving.Id))
            {
                return Result<Food>.Failure(FoodErrors.DuplicateServingSizeId);
            }

            var calculated = food.CalculateNutrition(serving);
            if (calculated.IsFailure)
            {
                return Result<Food>.Failure(calculated.Error!);
            }

            food._servingSizes.Add(serving.AtPosition(food._servingSizes.Count));
        }

        return Result<Food>.Success(food);
    }

    public Result<Food> Update(
        string? name,
        string? brand,
        decimal referenceQuantity,
        ReferenceUnit referenceUnit,
        NutritionalValues? nutritionalValues,
        IReadOnlyList<ServingSizeUpdate?>? servingSizes = null
    )
    {
        var servings = new List<ServingSize>();
        foreach (var input in servingSizes ?? [])
        {
            if (input is null)
            {
                return Result<Food>.Failure(FoodErrors.ServingSizeRequired);
            }

            if (input.Id is not null
                && !_servingSizes.Any(serving => serving.Id == input.Id))
            {
                return Result<Food>.Failure(FoodErrors.ServingSizeNotFound);
            }

            var serving = ServingSize.Create(
                input.Quantity,
                input.Unit,
                input.Label,
                input.EquivalentQuantity,
                input.Id
            );
            if (serving.IsFailure)
            {
                return Result<Food>.Failure(serving.Error!);
            }

            servings.Add(serving.Value);
        }

        // Validate the complete replacement before changing any existing entity.
        var candidate = Create(
            name,
            brand,
            referenceQuantity,
            referenceUnit,
            nutritionalValues,
            servings
        );
        if (candidate.IsFailure)
        {
            return Result<Food>.Failure(candidate.Error!);
        }

        var validated = candidate.Value;
        var replacements = new List<ServingSize>();
        foreach (var serving in validated.ServingSizes)
        {
            var existing =
                _servingSizes.SingleOrDefault(current => current.Id == serving.Id);
            if (existing is null)
            {
                replacements.Add(serving);
            }
            else
            {
                existing.Apply(serving);
                replacements.Add(existing);
            }
        }

        Name = validated.Name;
        Brand = validated.Brand;
        ReferenceQuantity = validated.ReferenceQuantity;
        ReferenceUnit = validated.ReferenceUnit;
        NutritionalValues = validated.NutritionalValues;
        _servingSizes.Clear();
        _servingSizes.AddRange(replacements);
        return Result<Food>.Success(this);
    }

    public Result<NutritionalValues> CalculateNutrition(ServingSize servingSize)
    {
        return NutritionalValues.Scale(servingSize.EquivalentQuantity, ReferenceQuantity);
    }
}
