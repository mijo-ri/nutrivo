using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Foods;

public static class FoodErrors
{
    public static readonly DomainError EmptyId = new(
        "Food.Id.Empty",
        "The food ID must not be empty."
    );

    public static readonly DomainError NameRequired = new(
        "Food.Name.Required",
        "The food name is required."
    );

    public static readonly DomainError NameTooLong = new(
        "Food.Name.TooLong",
        "The food name must not exceed 200 characters."
    );

    public static readonly DomainError BrandTooLong = new(
        "Food.Brand.TooLong",
        "The brand must not exceed 200 characters."
    );

    public static readonly DomainError InvalidReferenceUnit = new(
        "Food.ReferenceUnit.Invalid",
        "The reference unit must be grams or milliliters."
    );

    public static readonly DomainError NutritionalValuesRequired = new(
        "Food.NutritionalValues.Required",
        "Nutritional values are required."
    );

    public static readonly DomainError InvalidReferenceQuantity = new(
        "Food.ReferenceQuantity.Invalid",
        "The reference quantity must be positive."
    );

    public static readonly DomainError EmptyServingSizeId = new(
        "ServingSize.Id.Empty",
        "The serving size ID must not be empty."
    );

    public static readonly DomainError ServingSizeRequired = new(
        "Food.ServingSize.Required",
        "A serving size must not be null."
    );

    public static readonly DomainError DuplicateServingSizeId = new(
        "Food.ServingSize.Id.Duplicate",
        "Serving size IDs must be unique within a food."
    );

    public static readonly DomainError InvalidServingQuantity = new(
        "ServingSize.Quantity.Invalid",
        "The serving quantity must be positive."
    );

    public static readonly DomainError ServingUnitRequired = new(
        "ServingSize.Unit.Required",
        "The serving unit is required."
    );

    public static readonly DomainError ServingUnitTooLong = new(
        "ServingSize.Unit.TooLong",
        "The serving unit must not exceed 200 characters."
    );

    public static readonly DomainError ServingLabelTooLong = new(
        "ServingSize.Label.TooLong",
        "The serving label must not exceed 200 characters."
    );

    public static readonly DomainError InvalidEquivalentQuantity = new(
        "ServingSize.EquivalentQuantity.Invalid",
        "The equivalent quantity in the food reference unit must be positive."
    );

    public static readonly DomainError NutritionOutOfRange = new(
        "Food.ServingSize.Nutrition.OutOfRange",
        "The calculated serving nutrition exceeds the supported decimal range."
    );

    public static readonly DomainError NegativeKilocalories = new(
        "NutritionalValues.Kilocalories.Negative",
        "Kilocalories must not be negative."
    );

    public static readonly DomainError NegativeProtein = new(
        "NutritionalValues.ProteinGrams.Negative",
        "Protein grams must not be negative."
    );

    public static readonly DomainError NegativeCarbohydrates = new(
        "NutritionalValues.CarbohydratesGrams.Negative",
        "Carbohydrate grams must not be negative."
    );

    public static readonly DomainError NegativeFat = new(
        "NutritionalValues.FatGrams.Negative",
        "Fat grams must not be negative."
    );
}
