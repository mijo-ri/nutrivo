using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Diaries;

public static class DiaryEntryErrors
{
    public static readonly DomainError EmptyId = new(
        "DiaryEntry.Id.Empty",
        "The diary entry ID must not be empty."
    );

    public static readonly DomainError FoodRequired = new(
        "DiaryEntry.Food.Required",
        "A food is required."
    );

    public static readonly DomainError InvalidMealType = new(
        "DiaryEntry.MealType.Invalid",
        "The meal must be breakfast, lunch, dinner or snacks."
    );

    public static readonly DomainError InvalidQuantity = new(
        "DiaryEntry.Quantity.Invalid",
        "The consumed quantity must be positive."
    );

    public static readonly DomainError InvalidReferenceUnit = new(
        "DiaryEntry.ReferenceUnit.Invalid",
        "The consumed unit must be grams or milliliters."
    );

    public static readonly DomainError ReferenceUnitMismatch = new(
        "DiaryEntry.ReferenceUnit.Mismatch",
        "The consumed unit must match the food reference unit."
    );

    public static readonly DomainError ServingSizeIdRequired = new(
        "DiaryEntry.ServingSizeId.Required",
        "A serving size ID is required."
    );

    public static readonly DomainError ServingSizeNotFound = new(
        "DiaryEntry.ServingSize.NotFound",
        "The selected serving size does not belong to the food."
    );

    public static readonly DomainError InvalidServingCount = new(
        "DiaryEntry.ServingCount.Invalid",
        "The serving count must be positive."
    );

    public static readonly DomainError QuantityOutOfRange = new(
        "DiaryEntry.Quantity.OutOfRange",
        "The calculated consumed quantity is outside the supported decimal range."
    );

    public static readonly DomainError NutritionOutOfRange = new(
        "DiaryEntry.Nutrition.OutOfRange",
        "The calculated nutrition exceeds the supported decimal range."
    );
}
