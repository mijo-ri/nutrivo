namespace Nutrivo.Application.Targets.SetDailyNutritionTarget;

public sealed record SetDailyNutritionTargetCommand(
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams
);
