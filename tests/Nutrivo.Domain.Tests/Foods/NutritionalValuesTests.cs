using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Foods;

public class NutritionalValuesTests
{
    [Fact]
    public void Create_PreservesEveryDecimalWithoutRounding()
    {
        var result = NutritionalValues.Create(
            372.123456789m,
            13.567891234m,
            58.765432198m,
            7.012345678m
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(372.123456789m, result.Value.Kilocalories);
        Assert.Equal(13.567891234m, result.Value.ProteinGrams);
        Assert.Equal(58.765432198m, result.Value.CarbohydratesGrams);
        Assert.Equal(7.012345678m, result.Value.FatGrams);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create_WithNegativeNutrient_ReturnsSpecificError(int index)
    {
        decimal[] values = [372m, 13.5m, 58.7m, 7m];
        string[] expectedCodes =
        [
            "NutritionalValues.Kilocalories.Negative",
            "NutritionalValues.ProteinGrams.Negative",
            "NutritionalValues.CarbohydratesGrams.Negative",
            "NutritionalValues.FatGrams.Negative"
        ];
        values[index] = -0.000001m;

        var result = NutritionalValues.Create(values[0], values[1], values[2], values[3]);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCodes[index], result.Error?.Code);
    }

    [Theory]
    [InlineData(0, "NutritionalValues.Kilocalories.Negative")]
    [InlineData(1, "NutritionalValues.ProteinGrams.Negative")]
    [InlineData(2, "NutritionalValues.CarbohydratesGrams.Negative")]
    [InlineData(3, "NutritionalValues.FatGrams.Negative")]
    public void Create_WithMultipleNegativeNutrients_ReturnsFirstError(
        int firstNegativeIndex,
        string expectedCode
    )
    {
        decimal[] values = [0m, 0m, 0m, 0m];
        for (var index = firstNegativeIndex; index < values.Length; index++)
        {
            values[index] = -1m;
        }

        var result = NutritionalValues.Create(values[0], values[1], values[2], values[3]);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error?.Code);
    }

    [Fact]
    public void Create_WithLargestDecimalValues_DoesNotImposeUpperBoundsOrSumValues()
    {
        var result = NutritionalValues.Create(
            decimal.MaxValue,
            decimal.MaxValue,
            decimal.MaxValue,
            decimal.MaxValue
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.MaxValue, result.Value.Kilocalories);
        Assert.Equal(decimal.MaxValue, result.Value.ProteinGrams);
        Assert.Equal(decimal.MaxValue, result.Value.CarbohydratesGrams);
        Assert.Equal(decimal.MaxValue, result.Value.FatGrams);
    }

    [Fact]
    public void
        Equality_WithEquivalentValues_IgnoresDecimalScaleAndSupportsHashCollections()
    {
        var first = NutritionalValues.Create(100m, 1m, 2m, 3m).Value;
        var second = NutritionalValues.Create(100.00m, 1.0m, 2.00m, 3.000m).Value;

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<NutritionalValues> { first, second });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Equality_WhenAnyNutrientDiffers_ReturnsFalse(int index)
    {
        var first = NutritionalValues.Create(100m, 1m, 2m, 3m).Value;
        decimal[] values = [100m, 1m, 2m, 3m];
        values[index] += 0.01m;
        var second = NutritionalValues.Create(
            values[0],
            values[1],
            values[2],
            values[3]
        ).Value;

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }
}
