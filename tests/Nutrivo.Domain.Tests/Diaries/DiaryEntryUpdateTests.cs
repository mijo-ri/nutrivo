using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Diaries;

public sealed class DiaryEntryUpdateTests
{
    private static Food Food(
        ReferenceUnit unit = ReferenceUnit.Gram,
        decimal calories = 200.005m,
        decimal equivalent = 116m
    ) =>
        Nutrivo.Domain.Foods.Food.Create(
                "Original",
                "Brand",
                100m,
                unit,
                NutritionalValues.Create(calories, 10.005m, 20.005m, 5.005m).Value,
                [ServingSize.Create(2m, "pieces", "original", equivalent).Value]
            )
            .Value;

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public void DirectUpdate_PreservesHistoryAndUsesOriginalPrecision(ReferenceUnit unit)
    {
        var food = Food(unit);
        var entry = DiaryEntry.Create(
                food,
                new DateOnly(2026, 11, 14),
                MealType.Lunch,
                50m,
                unit
            )
            .Value;
        var id = entry.Id;
        var snapshot = entry.FoodSnapshot;
        food.Update(
            "Changed",
            null,
            100m,
            unit,
            NutritionalValues.Create(999m, 0m, 0m, 0m).Value,
            []
        );

        Assert.True(entry.UpdateQuantity(75m).IsSuccess);
        Assert.Equal(150.00375m, entry.NutritionalValues.Kilocalories);
        Assert.True(entry.UpdateQuantity(25m).IsSuccess);
        Assert.Equal(50.00125m, entry.NutritionalValues.Kilocalories);
        Assert.Equal(id, entry.Id);
        Assert.Same(snapshot, entry.FoodSnapshot);
        Assert.Equal("Original", snapshot.Name);
        Assert.Equal(new DateOnly(2026, 11, 14), entry.Date);
        Assert.Equal(MealType.Lunch, entry.MealType);
        Assert.Equal(unit, entry.ReferenceUnit);
        Assert.Null(entry.ServingCount);
    }

    [Fact]
    public void ServingUpdate_UsesCapturedServingAndKeepsSnapshots()
    {
        var food = Food();
        var entry = DiaryEntry.CreateFromServing(
                food,
                new DateOnly(2026, 11, 14),
                MealType.Breakfast,
                food.ServingSizes.Single().Id,
                1m
            )
            .Value;
        var snapshot = entry.ServingSizeSnapshot;
        Assert.True(entry.UpdateServingCount(0.5m).IsSuccess);
        Assert.Equal(58m, entry.Quantity);
        Assert.Equal(0.5m, entry.ServingCount);
        Assert.Equal(116.0029m, entry.NutritionalValues.Kilocalories);
        Assert.Same(snapshot, entry.ServingSizeSnapshot);
    }

    [Fact]
    public void RejectedUpdates_DoNotMutateAnyConsumedValue()
    {
        var food = Food(calories: decimal.MaxValue / 2);
        var direct = DiaryEntry.Create(
                food,
                new DateOnly(2026, 11, 14),
                MealType.Dinner,
                100m,
                ReferenceUnit.Gram
            )
            .Value;
        var before = direct.NutritionalValues;
        Assert.Equal(DiaryEntryErrors.InvalidQuantity, direct.UpdateQuantity(0).Error);
        Assert.Equal(DiaryEntryErrors.InvalidQuantity, direct.UpdateQuantity(-1).Error);
        Assert.Equal(
            DiaryEntryErrors.AmountModeMismatch,
            direct.UpdateServingCount(1).Error
        );
        Assert.Equal(
            DiaryEntryErrors.NutritionOutOfRange,
            direct.UpdateQuantity(300m).Error
        );
        Assert.Equal(100m, direct.Quantity);
        Assert.Same(before, direct.NutritionalValues);
        Assert.Null(direct.ServingCount);
    }

    [Theory]
    [InlineData("116", "79228162514264337593543950335")]
    [InlineData("0.0000000000000000000000000001", "0.1")]
    public void UnrepresentableServingQuantity_DoesNotMutate(
        string equivalent,
        string count
    )
    {
        var food = Food(
            equivalent: decimal.Parse(
                equivalent,
                System.Globalization.CultureInfo.InvariantCulture
            )
        );
        var entry = DiaryEntry.CreateFromServing(
                food,
                new DateOnly(2026, 11, 14),
                MealType.Snacks,
                food.ServingSizes.Single().Id,
                1m
            )
            .Value;
        var quantity = entry.Quantity;
        var nutrition = entry.NutritionalValues;
        Assert.Equal(DiaryEntryErrors.AmountModeMismatch, entry.UpdateQuantity(1).Error);
        Assert.Equal(
            DiaryEntryErrors.InvalidServingCount,
            entry.UpdateServingCount(0).Error
        );
        Assert.Equal(
            DiaryEntryErrors.InvalidServingCount,
            entry.UpdateServingCount(-1).Error
        );
        Assert.Equal(
            DiaryEntryErrors.QuantityOutOfRange,
            entry.UpdateServingCount(
                    decimal.Parse(
                        count,
                        System.Globalization.CultureInfo.InvariantCulture
                    )
                )
                .Error
        );
        Assert.Equal(quantity, entry.Quantity);
        Assert.Equal(1m, entry.ServingCount);
        Assert.Same(nutrition, entry.NutritionalValues);
    }
}
