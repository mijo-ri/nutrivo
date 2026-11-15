using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Targets;

public sealed class DailyNutritionTarget : AggregateRoot<int>
{
    private DailyNutritionTarget(
        decimal kilocalories,
        decimal proteinGrams,
        decimal carbohydratesGrams,
        decimal fatGrams
    ) : base(1)
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

    public static Result<DailyNutritionTarget> Create(
        decimal kilocalories,
        decimal proteinGrams,
        decimal carbohydratesGrams,
        decimal fatGrams
    )
    {
        if (kilocalories < 0)
        {
            return Result<DailyNutritionTarget>.Failure(
                DailyNutritionTargetErrors.NegativeKilocalories
            );
        }

        if (proteinGrams < 0)
        {
            return Result<DailyNutritionTarget>.Failure(
                DailyNutritionTargetErrors.NegativeProtein
            );
        }

        if (carbohydratesGrams < 0)
        {
            return Result<DailyNutritionTarget>.Failure(
                DailyNutritionTargetErrors.NegativeCarbohydrates
            );
        }

        if (fatGrams < 0)
        {
            return Result<DailyNutritionTarget>.Failure(
                DailyNutritionTargetErrors.NegativeFat
            );
        }

        return Result<DailyNutritionTarget>.Success(
            new DailyNutritionTarget(
                kilocalories,
                proteinGrams,
                carbohydratesGrams,
                fatGrams
            )
        );
    }
}
