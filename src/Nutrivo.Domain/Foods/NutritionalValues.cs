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
}
