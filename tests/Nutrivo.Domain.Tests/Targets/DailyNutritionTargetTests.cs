using Nutrivo.Domain.Targets;

namespace Nutrivo.Domain.Tests.Targets;

public sealed class DailyNutritionTargetTests
{
    [Theory]
    [InlineData(0, "Kilocalories")]
    [InlineData(1, "ProteinGrams")]
    [InlineData(2, "CarbohydratesGrams")]
    [InlineData(3, "FatGrams")]
    public void Create_RejectsEachNegativeField(int index, string field)
    {
        decimal[] values = [2400m, 180m, 260m, 70m];
        values[index] = -0.0000000000000000000000000001m;

        var result = DailyNutritionTarget.Create(
            values[0],
            values[1],
            values[2],
            values[3]
        );

        Assert.True(result.IsFailure);
        Assert.Equal($"DailyNutritionTarget.{field}.Negative", result.Error!.Code);
    }

    [Fact]
    public void Create_PreservesIndependentFractionalValues()
    {
        var target = DailyNutritionTarget
            .Create(2400.123456789m, 180.5m, 260.25m, 70.125m)
            .Value;

        Assert.Equal(1, target.Id);
        Assert.Equal(2400.123456789m, target.Kilocalories);
        Assert.Equal(180.5m, target.ProteinGrams);
        Assert.Equal(260.25m, target.CarbohydratesGrams);
        Assert.Equal(70.125m, target.FatGrams);
    }

    [Fact]
    public void Create_AcceptsExplicitZeroSet()
    {
        var result = DailyNutritionTarget.Create(0m, 0m, 0m, 0m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Kilocalories);
        Assert.Equal(0m, result.Value.ProteinGrams);
        Assert.Equal(0m, result.Value.CarbohydratesGrams);
        Assert.Equal(0m, result.Value.FatGrams);
    }

    [Fact]
    public void Create_AcceptsMixedZeroAndPositiveBoundariesWithoutCalculatingEnergy()
    {
        var result = DailyNutritionTarget.Create(
            decimal.MaxValue,
            0m,
            0.0000000000000000000000000001m,
            0m
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.MaxValue, result.Value.Kilocalories);
        Assert.Equal(0m, result.Value.ProteinGrams);
        Assert.Equal(0.0000000000000000000000000001m, result.Value.CarbohydratesGrams);
    }

    [Fact]
    public void Create_AcceptsMaximumDecimalsInEveryField()
    {
        var result = DailyNutritionTarget.Create(
            decimal.MaxValue,
            decimal.MaxValue,
            decimal.MaxValue,
            decimal.MaxValue
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.MaxValue, result.Value.ProteinGrams);
        Assert.Equal(decimal.MaxValue, result.Value.CarbohydratesGrams);
        Assert.Equal(decimal.MaxValue, result.Value.FatGrams);
    }
}
