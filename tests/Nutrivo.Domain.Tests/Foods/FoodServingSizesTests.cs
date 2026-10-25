using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Foods;

public sealed class FoodServingSizesTests
{
    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public void Create_WithArbitraryBasisAndMultipleServings_PreservesOrderAndCalculates(
        ReferenceUnit unit
    )
    {
        var small = ServingSize.Create(1m, " Stück ", " Klein ", 45m).Value;
        var mediumPair = ServingSize.Create(2m, "Stück", "Mittel", 116m).Value;
        var nutrition = NutritionalValues.Create(150m, 10m, 1m, 5m).Value;
        var inputs = new List<ServingSize?> { small, mediumPair };

        var food = Food.Create("Egg", null, 50m, unit, nutrition, inputs).Value;
        inputs.Clear();

        Assert.Equal(50m, food.ReferenceQuantity);
        Assert.Equal(
            new[] { small.Id, mediumPair.Id },
            food.ServingSizes.Select(s => s.Id)
        );
        Assert.Equal(new[] { 0, 1 }, food.ServingSizes.Select(s => s.Position));
        Assert.Equal("Stück", food.ServingSizes[0].Unit);
        Assert.Equal("Klein", food.ServingSizes[0].Label);
        Assert.Equal(
            348m,
            food.CalculateNutrition(food.ServingSizes[1]).Value.Kilocalories
        );
        Assert.Equal(
            23.2m,
            food.CalculateNutrition(food.ServingSizes[1]).Value.ProteinGrams
        );
        Assert.Equal(
            2.32m,
            food.CalculateNutrition(food.ServingSizes[1]).Value.CarbohydratesGrams
        );
        Assert.Equal(11.6m, food.CalculateNutrition(food.ServingSizes[1]).Value.FatGrams);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ServingSize>)food.ServingSizes).Clear()
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonpositiveBasis_ReturnsError(int quantity)
    {
        var result = Food.Create(
            "Egg",
            null,
            quantity,
            ReferenceUnit.Gram,
            NutritionalValues.Create(1m, 0m, 0m, 0m).Value
        );

        Assert.Same(FoodErrors.InvalidReferenceQuantity, result.Error);
    }

    [Fact]
    public void Create_WithoutServings_UsesEmptyCollection()
    {
        var food = Food.Create(
                "Egg",
                null,
                0.5m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(0m, 0m, 0m, 0m).Value
            )
            .Value;

        Assert.Empty(food.ServingSizes);
    }

    [Fact]
    public void Create_WithNullOrDuplicateServing_ReturnsError()
    {
        var nutrition = NutritionalValues.Create(1m, 0m, 0m, 0m).Value;
        var serving = ServingSize.Create(1m, "piece", null, 1m).Value;
        Assert.Same(
            FoodErrors.ServingSizeRequired,
            Food.Create("Egg", null, 100m, ReferenceUnit.Gram, nutrition, [null]).Error
        );
        Assert.Same(
            FoodErrors.DuplicateServingSizeId,
            Food.Create(
                    "Egg",
                    null,
                    100m,
                    ReferenceUnit.Gram,
                    nutrition,
                    [serving, serving]
                )
                .Error
        );
    }

    [Fact]
    public void Create_WithOverflowingServing_ReturnsError()
    {
        var serving = ServingSize.Create(1m, "piece", null, 2m).Value;
        var result = Food.Create(
            "Egg",
            null,
            1m,
            ReferenceUnit.Gram,
            NutritionalValues.Create(decimal.MaxValue, 0m, 0m, 0m).Value,
            [serving]
        );

        Assert.Same(FoodErrors.NutritionOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n")]
    public void Serving_WithBlankLabel_NormalizesItToNull(string? label)
    {
        var serving = ServingSize.Create(0.5m, " Esslöffel ", label, 7.5m).Value;

        Assert.Equal("Esslöffel", serving.Unit);
        Assert.Null(serving.Label);
        Assert.Equal(0.5m, serving.Quantity);
        Assert.Equal(7.5m, serving.EquivalentQuantity);
    }

    [Theory]
    [InlineData(0, "piece", null, 1, "ServingSize.Quantity.Invalid")]
    [InlineData(-1, "piece", null, 1, "ServingSize.Quantity.Invalid")]
    [InlineData(1, null, null, 1, "ServingSize.Unit.Required")]
    [InlineData(1, " \t", null, 1, "ServingSize.Unit.Required")]
    [InlineData(1, "piece", null, 0, "ServingSize.EquivalentQuantity.Invalid")]
    [InlineData(1, "piece", null, -1, "ServingSize.EquivalentQuantity.Invalid")]
    public void Serving_WithInvalidInput_ReturnsError(
        int quantity,
        string? unit,
        string? label,
        int equivalent,
        string code
    )
    {
        Assert.Equal(
            code,
            ServingSize.Create(quantity, unit, label, equivalent).Error?.Code
        );
    }

    [Fact]
    public void Serving_ValidatesTextLengthsAfterTrimming()
    {
        Assert.True(
            ServingSize.Create(
                    1m,
                    $" {new string('a', 200)} ",
                    $" {new string('b', 200)} ",
                    1m
                )
                .IsSuccess
        );
        Assert.Same(
            FoodErrors.ServingUnitTooLong,
            ServingSize.Create(1m, new string('a', 201), null, 1m).Error
        );
        Assert.Same(
            FoodErrors.ServingLabelTooLong,
            ServingSize.Create(1m, "piece", new string('b', 201), 1m).Error
        );
    }

    [Fact]
    public void ServingId_HasTypedValueSemanticsAndRejectsEmptyGuid()
    {
        var id = ServingSizeId.New();

        Assert.Equal(id, ServingSizeId.Create(id.Value).Value);
        Assert.NotEqual(id, ServingSizeId.New());
        Assert.False(id.Equals(FoodId.Create(id.Value).Value));
        Assert.Same(
            FoodErrors.EmptyServingSizeId,
            ServingSizeId.Create(Guid.Empty).Error
        );
    }

    public static TheoryData<decimal, decimal, decimal, decimal> ScalingCases => new()
    {
        { 150m, 58m, 100m, 87m },
        { 1.005m, 1m, 1m, 1.005m },
        { 0m, decimal.MaxValue, 0.0000000000000000000000000001m, 0m },
        { decimal.MaxValue, 100m, 100m, decimal.MaxValue },
        { decimal.MaxValue, 2m, 4m, decimal.MaxValue / 4m * 2m },
        {
            0.0000000000000000000000000001m,
            0.0000000000000000000000000003m,
            0.0000000000000000000000000002m,
            0.0000000000000000000000000002m
        }
    };

    [Theory]
    [MemberData(nameof(ScalingCases))]
    public void Scale_PreservesDecimalPrecisionWithoutDisplayRounding(
        decimal value,
        decimal equivalent,
        decimal reference,
        decimal expected
    )
    {
        var nutrition = NutritionalValues.Create(value, value, value, value).Value;
        var result = nutrition.Scale(equivalent, reference);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            NutritionalValues.Create(expected, expected, expected, expected).Value,
            result.Value
        );
        Assert.Equal(value, nutrition.Kilocalories);
    }

    [Theory]
    [InlineData(0, 1, "ServingSize.EquivalentQuantity.Invalid")]
    [InlineData(1, 0, "Food.ReferenceQuantity.Invalid")]
    public void Scale_WithInvalidQuantities_ReturnsError(
        int equivalent,
        int reference,
        string code
    )
    {
        var result = NutritionalValues
            .Create(1m, 0m, 0m, 0m)
            .Value.Scale(equivalent, reference);
        Assert.Equal(code, result.Error?.Code);
    }

    [Theory]
    [InlineData("1.005", "1.01")]
    [InlineData("1.0049", "1.00")]
    [InlineData("1.015", "1.02")]
    [InlineData("0.005", "0.01")]
    public void DisplayRounding_UsesTwoPlacesAndMidpointsAwayFromZero(
        string input,
        string expected
    )
    {
        Assert.Equal(
            decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            NutritionRounding.ForDisplay(
                decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)
            )
        );
    }
}
