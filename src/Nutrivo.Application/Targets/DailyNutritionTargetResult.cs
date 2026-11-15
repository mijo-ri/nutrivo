using Nutrivo.Domain.Targets;

namespace Nutrivo.Application.Targets;

public sealed record DailyNutritionTargetResult(DailyNutritionTargetValues? Targets);

public sealed record DailyNutritionTargetValues(
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams
)
{
    public static DailyNutritionTargetValues From(DailyNutritionTarget target)
    {
        return new DailyNutritionTargetValues(
            target.Kilocalories,
            target.ProteinGrams,
            target.CarbohydratesGrams,
            target.FatGrams
        );
    }
}
