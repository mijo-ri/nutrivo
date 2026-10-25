using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Foods;

public class FoodTests
{
    private static NutritionalValues OatNutrition()
    {
        return NutritionalValues.Create(372m, 13.5m, 58.7m, 7m).Value;
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public void Create_WithSupportedUnit_PreservesNutritionAndUsesReferenceQuantityOf100(
        ReferenceUnit unit
    )
    {
        var nutrition = OatNutrition();

        var result = Food.Create("Oats", "Example", 100m, unit, nutrition);

        Assert.True(result.IsSuccess);
        Assert.Equal("Oats", result.Value.Name);
        Assert.Equal("Example", result.Value.Brand);
        Assert.Equal(100m, result.Value.ReferenceQuantity);
        Assert.Equal(unit, result.Value.ReferenceUnit);
        Assert.Equal(nutrition, result.Value.NutritionalValues);
        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
    }

    [Fact]
    public void Create_TrimsOnlyOuterWhitespace()
    {
        var result = Food.Create(
            " \tRolled  oats\n",
            "\nExample  brand \t",
            100m,
            ReferenceUnit.Gram,
            OatNutrition()
        );

        Assert.Equal("Rolled  oats", result.Value.Name);
        Assert.Equal("Example  brand", result.Value.Brand);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n")]
    public void Create_WithoutName_ReturnsRequiredError(string? name)
    {
        var result = Food.Create(name, null, 100m, ReferenceUnit.Gram, OatNutrition());

        Assert.True(result.IsFailure);
        Assert.Equal("Food.Name.Required", result.Error?.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public void Create_WithNameAtLengthBoundary_SucceedsAfterTrimming(int length)
    {
        var name = new string('a', length);

        var result = Food.Create(
            $" {name} ",
            null,
            100m,
            ReferenceUnit.Gram,
            OatNutrition()
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public void Create_WithNameLongerThan200_ReturnsError()
    {
        var result = Food.Create(
            new string('a', 201),
            null,
            100m,
            ReferenceUnit.Gram,
            OatNutrition()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Food.Name.TooLong", result.Error?.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n")]
    public void Create_WithoutBrand_NormalizesBrandToNull(string? brand)
    {
        var result = Food.Create("Oats", brand, 100m, ReferenceUnit.Gram, OatNutrition());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Brand);
    }

    [Fact]
    public void Create_With200CharacterBrand_SucceedsAfterTrimming()
    {
        var brand = new string('a', 200);

        var result = Food.Create(
            "Oats",
            $" {brand} ",
            100m,
            ReferenceUnit.Gram,
            OatNutrition()
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(brand, result.Value.Brand);
    }

    [Fact]
    public void Create_WithBrandLongerThan200_ReturnsError()
    {
        var result = Food.Create(
            "Oats",
            new string('a', 201),
            100m,
            ReferenceUnit.Gram,
            OatNutrition()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Food.Brand.TooLong", result.Error?.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void Create_WithUnsupportedUnit_ReturnsError(int unit)
    {
        var result = Food.Create("Oats", null, 100m, (ReferenceUnit)unit, OatNutrition());

        Assert.True(result.IsFailure);
        Assert.Equal("Food.ReferenceUnit.Invalid", result.Error?.Code);
    }

    [Fact]
    public void Create_WithoutNutrition_ReturnsError()
    {
        var result = Food.Create("Oats", null, 100m, ReferenceUnit.Gram, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Food.NutritionalValues.Required", result.Error?.Code);
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public void Create_WithZeroNutrition_Succeeds(ReferenceUnit unit)
    {
        var nutrition = NutritionalValues.Create(0m, 0m, 0m, 0m).Value;

        var result = Food.Create("Water", null, 100m, unit, nutrition);

        Assert.True(result.IsSuccess);
        Assert.Equal(nutrition, result.Value.NutritionalValues);
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public void Create_WithMacroSumAbove100AndIndependentCalories_Succeeds(
        ReferenceUnit unit
    )
    {
        var nutrition = NutritionalValues.Create(1m, 60m, 60m, 60m).Value;

        var result = Food.Create("Product", null, 100m, unit, nutrition);

        Assert.True(result.IsSuccess);
        Assert.Equal(1m, result.Value.NutritionalValues.Kilocalories);
        Assert.Equal(60m, result.Value.NutritionalValues.FatGrams);
    }

    [Fact]
    public void Create_WithIdenticalData_ProducesDistinctFoodIdentities()
    {
        var nutrition = OatNutrition();

        var first = Food.Create("Oats", "Example", 100m, ReferenceUnit.Gram, nutrition)
            .Value;
        var second = Food.Create("Oats", "Example", 100m, ReferenceUnit.Gram, nutrition)
            .Value;

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first, second);
        Assert.False(first == second);
    }

    [Theory]
    [InlineData(null, 201, 0, "Food.Name.Required")]
    [InlineData("", 201, 0, "Food.Name.Required")]
    [InlineData("too-long", 201, 0, "Food.Name.TooLong")]
    [InlineData("Oats", 201, 0, "Food.Brand.TooLong")]
    [InlineData("Oats", 0, 0, "Food.ReferenceUnit.Invalid")]
    [InlineData("Oats", 0, 1, "Food.NutritionalValues.Required")]
    public void Create_WithMultipleInvalidInputs_ReturnsFirstError(
        string? name,
        int brandLength,
        int unit,
        string expectedCode
    )
    {
        if (name == "too-long")
        {
            name = new string('a', 201);
        }

        var result = Food.Create(
            name,
            new string('b', brandLength),
            100m,
            (ReferenceUnit)unit,
            null
        );

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error?.Code);
    }
}
