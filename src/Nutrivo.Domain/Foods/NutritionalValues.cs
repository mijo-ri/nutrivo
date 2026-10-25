using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Foods;

public sealed class NutritionalValues : ValueObject
{
    private NutritionalValues(
        decimal kilocalories,
        decimal proteinGrams,
        decimal carbohydratesGrams,
        decimal fatGrams
    )
    {
        Kilocalories = kilocalories;
        ProteinGrams = proteinGrams;
        CarbohydratesGrams = carbohydratesGrams;
        FatGrams = fatGrams;
    }

    public decimal Kilocalories { get; }

    public decimal ProteinGrams { get; }

    public decimal CarbohydratesGrams { get; }

    public decimal FatGrams { get; }

    public static Result<NutritionalValues> Create(
        decimal kilocalories,
        decimal proteinGrams,
        decimal carbohydratesGrams,
        decimal fatGrams
    )
    {
        if (kilocalories < 0)
        {
            return Result<NutritionalValues>.Failure(FoodErrors.NegativeKilocalories);
        }

        if (proteinGrams < 0)
        {
            return Result<NutritionalValues>.Failure(FoodErrors.NegativeProtein);
        }

        if (carbohydratesGrams < 0)
        {
            return Result<NutritionalValues>.Failure(FoodErrors.NegativeCarbohydrates);
        }

        if (fatGrams < 0)
        {
            return Result<NutritionalValues>.Failure(FoodErrors.NegativeFat);
        }

        return Result<NutritionalValues>.Success(
            new NutritionalValues(
                kilocalories,
                proteinGrams,
                carbohydratesGrams,
                fatGrams
            )
        );
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kilocalories;
        yield return ProteinGrams;
        yield return CarbohydratesGrams;
        yield return FatGrams;
    }

    public Result<NutritionalValues> Scale(
        decimal equivalentQuantity,
        decimal referenceQuantity
    )
    {
        if (referenceQuantity <= 0)
        {
            return Result<NutritionalValues>.Failure(FoodErrors.InvalidReferenceQuantity);
        }

        if (equivalentQuantity <= 0)
        {
            return Result<NutritionalValues>.Failure(
                FoodErrors.InvalidEquivalentQuantity
            );
        }

        try
        {
            return Result<NutritionalValues>.Success(
                new NutritionalValues(
                    ScaleValue(Kilocalories, equivalentQuantity, referenceQuantity),
                    ScaleValue(ProteinGrams, equivalentQuantity, referenceQuantity),
                    ScaleValue(CarbohydratesGrams, equivalentQuantity, referenceQuantity),
                    ScaleValue(FatGrams, equivalentQuantity, referenceQuantity)
                )
            );
        }
        catch (OverflowException)
        {
            return Result<NutritionalValues>.Failure(FoodErrors.NutritionOutOfRange);
        }
    }

    private static decimal ScaleValue(
        decimal value,
        decimal equivalent,
        decimal reference
    )
    {
        if (equivalent == reference || value == 0)
        {
            return value;
        }

        // Divide first for fractional bases to avoid underflow of the product.
        // Retry in the other order if only the intermediate result overflows.
        try
        {
            return reference < 1m
                ? value / reference * equivalent
                : value * equivalent / reference;
        }
        catch (OverflowException)
        {
            return reference < 1m
                ? value * equivalent / reference
                : value / reference * equivalent;
        }
    }
}
