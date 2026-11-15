using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Targets;

public static class DailyNutritionTargetErrors
{
    public static readonly DomainError NegativeKilocalories = new(
        "DailyNutritionTarget.Kilocalories.Negative",
        "The calorie target must not be negative."
    );

    public static readonly DomainError NegativeProtein = new(
        "DailyNutritionTarget.ProteinGrams.Negative",
        "The protein target must not be negative."
    );

    public static readonly DomainError NegativeCarbohydrates = new(
        "DailyNutritionTarget.CarbohydratesGrams.Negative",
        "The carbohydrate target must not be negative."
    );

    public static readonly DomainError NegativeFat = new(
        "DailyNutritionTarget.FatGrams.Negative",
        "The fat target must not be negative."
    );
}
