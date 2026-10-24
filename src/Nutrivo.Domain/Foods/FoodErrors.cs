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
