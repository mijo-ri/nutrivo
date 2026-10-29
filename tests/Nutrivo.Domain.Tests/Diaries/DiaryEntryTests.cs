using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Diaries;

public sealed class DiaryEntryTests
{
    private static readonly DateOnly Date = new(2026, 10, 25);

    [Theory]
    [InlineData(MealType.Breakfast)]
    [InlineData(MealType.Lunch)]
    [InlineData(MealType.Dinner)]
    [InlineData(MealType.Snacks)]
    public void Factories_WithValidMeal_PreserveDateAndMeal(MealType mealType)
    {
        var serving = ServingSize.Create(2m, "eggs", "medium", 116m).Value;
        var food = CreateFood(servings: [serving]);

        var direct = DiaryEntry.Create(food, Date, mealType, 58m, ReferenceUnit.Gram)
            .Value;
        var portion = DiaryEntry.CreateFromServing(
                food,
                Date,
                mealType,
                serving.Id,
                0.5m
            )
            .Value;

        Assert.Equal(Date, direct.Date);
        Assert.Equal(Date, portion.Date);
        Assert.Equal(mealType, direct.MealType);
        Assert.Equal(mealType, portion.MealType);
        Assert.Equal(direct.NutritionalValues, portion.NutritionalValues);
    }

    [Theory]
    [InlineData(2000, 2, 29)]
    [InlineData(2100, 12, 31)]
    public void Factories_AllowPastAndFutureCalendarDates(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        var serving = ServingSize.Create(1m, "piece", null, 58m).Value;
        var food = CreateFood(servings: [serving]);

        Assert.Equal(
            date,
            DiaryEntry.Create(food, date, MealType.Lunch, 58m, ReferenceUnit.Gram)
                .Value.Date
        );
        Assert.Equal(
            date,
            DiaryEntry.CreateFromServing(food, date, MealType.Lunch, serving.Id, 1m)
                .Value.Date
        );
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public void Create_WithFractionalQuantityAndArbitraryBasis_ScalesAllNutrients(
        ReferenceUnit unit
    )
    {
        var food = CreateFood(unit, 50m);

        var entry = DiaryEntry.Create(food, Date, MealType.Lunch, 12.5m, unit).Value;

        Assert.Equal(12.5m, entry.Quantity);
        Assert.Equal(unit, entry.ReferenceUnit);
        Assert.Equal(
            NutritionalValues.Create(37.5m, 2.5m, 0.25m, 1.25m).Value,
            entry.NutritionalValues
        );
        Assert.Null(entry.ServingSizeSnapshot);
        Assert.Null(entry.ServingCount);
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public void Create_WithFractionalReferenceBasis_PreservesUnroundedNutrition(
        ReferenceUnit unit
    )
    {
        var food = CreateFood(
            unit,
            0.5m,
            NutritionalValues.Create(1.005m, 2.345m, 3.456m, 4.567m).Value
        );

        var entry = DiaryEntry.Create(food, Date, MealType.Snacks, 0.25m, unit).Value;

        Assert.Equal(
            NutritionalValues.Create(0.5025m, 1.1725m, 1.728m, 2.2835m).Value,
            entry.NutritionalValues
        );
        Assert.Equal(1.005m, entry.FoodSnapshot.NutritionalValues.Kilocalories);
    }

    public static TheoryData<decimal, decimal, decimal> ServingCases => new()
    {
        { 3m, 348m, 522m },
        { 0.5m, 58m, 87m },
        { 2.5m, 290m, 435m }
    };

    [Theory]
    [MemberData(nameof(ServingCases))]
    public void CreateFromServing_CountMultipliesEntireServing(
        decimal count,
        decimal expectedQuantity,
        decimal expectedCalories
    )
    {
        var serving = ServingSize.Create(2m, "eggs", "medium", 116m).Value;
        var food = CreateFood(servings: [serving]);
        var reconstructedId = ServingSizeId.Create(serving.Id.Value).Value;

        var entry = DiaryEntry.CreateFromServing(
                food,
                Date,
                MealType.Breakfast,
                reconstructedId,
                count
            )
            .Value;

        Assert.Equal(expectedQuantity, entry.Quantity);
        Assert.Equal(expectedCalories, entry.NutritionalValues.Kilocalories);
        Assert.Equal(count, entry.ServingCount);
        Assert.Equal(ReferenceUnit.Gram, entry.ReferenceUnit);
        var snapshot = Assert.IsType<ServingSizeSnapshot>(entry.ServingSizeSnapshot);
        Assert.Equal(2m, snapshot.Quantity);
        Assert.Equal(116m, snapshot.EquivalentQuantity);
    }

    [Fact]
    public void CreateFromServing_SelectsRequestedServingAndScalesWithoutRounding()
    {
        var small = ServingSize.Create(1m, "cup", "small", 0.5m).Value;
        var large = ServingSize.Create(1m, "cup", "large", 1m).Value;
        var nutrition = NutritionalValues.Create(1.005m, 2.345m, 3.456m, 4.567m).Value;
        var food = CreateFood(ReferenceUnit.Milliliter, 2m, nutrition, [small, large]);

        var entry = DiaryEntry.CreateFromServing(
                food,
                Date,
                MealType.Snacks,
                large.Id,
                0.5m
            )
            .Value;

        Assert.Equal(0.5m, entry.Quantity);
        Assert.Equal(ReferenceUnit.Milliliter, entry.ReferenceUnit);
        Assert.Equal(
            NutritionalValues.Create(0.25125m, 0.58625m, 0.864m, 1.14175m).Value,
            entry.NutritionalValues
        );
        Assert.Equal(large.Id, entry.ServingSizeSnapshot?.ServingSizeId);
        Assert.Equal("large", entry.ServingSizeSnapshot?.Label);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Farm", "medium")]
    public void Factories_CaptureFoodAndSelectedServingDetails(
        string? brand,
        string? label
    )
    {
        var serving = ServingSize.Create(2m, "eggs", label, 116m).Value;
        var nutrition = NutritionalValues.Create(150m, 10m, 1m, 5m).Value;
        var food = Food.Create(
                "Egg",
                brand,
                100m,
                ReferenceUnit.Gram,
                nutrition,
                [serving]
            )
            .Value;
        var direct = DiaryEntry.Create(
                food,
                Date,
                MealType.Breakfast,
                116m,
                ReferenceUnit.Gram
            )
            .Value;
        var entry = DiaryEntry.CreateFromServing(
                food,
                Date,
                MealType.Breakfast,
                serving.Id,
                1m
            )
            .Value;

        Assert.Equal(food.Id, entry.FoodSnapshot.FoodId);
        Assert.Equal("Egg", entry.FoodSnapshot.Name);
        Assert.Equal(brand, entry.FoodSnapshot.Brand);
        Assert.Equal(100m, entry.FoodSnapshot.ReferenceQuantity);
        Assert.Equal(ReferenceUnit.Gram, entry.FoodSnapshot.ReferenceUnit);
        Assert.Equal(nutrition, entry.FoodSnapshot.NutritionalValues);
        Assert.Equal(direct.FoodSnapshot, entry.FoodSnapshot);
        Assert.NotSame(direct.FoodSnapshot, entry.FoodSnapshot);

        var snapshot = Assert.IsType<ServingSizeSnapshot>(entry.ServingSizeSnapshot);
        Assert.Equal(serving.Id, snapshot.ServingSizeId);
        Assert.Equal(2m, snapshot.Quantity);
        Assert.Equal("eggs", snapshot.Unit);
        Assert.Equal(label, snapshot.Label);
        Assert.Equal(116m, snapshot.EquivalentQuantity);
    }

    [Fact]
    public void Snapshots_HaveValueEqualityAcrossSeparateEntries()
    {
        var serving = ServingSize.Create(2m, "eggs", "medium", 116m).Value;
        var food = CreateFood(servings: [serving]);
        var first = DiaryEntry.CreateFromServing(
                food,
                Date,
                MealType.Breakfast,
                serving.Id,
                1m
            )
            .Value;
        var second = DiaryEntry.CreateFromServing(
                food,
                Date.AddDays(1),
                MealType.Dinner,
                serving.Id,
                3m
            )
            .Value;

        Assert.Equal(first.FoodSnapshot, second.FoodSnapshot);
        Assert.Equal(first.FoodSnapshot.GetHashCode(), second.FoodSnapshot.GetHashCode());
        Assert.Equal(first.ServingSizeSnapshot, second.ServingSizeSnapshot);
        Assert.Equal(
            first.ServingSizeSnapshot!.GetHashCode(),
            second.ServingSizeSnapshot!.GetHashCode()
        );
        Assert.NotSame(first.ServingSizeSnapshot, second.ServingSizeSnapshot);
        Assert.Equal(116m, first.Quantity);
        Assert.Equal(174m, first.NutritionalValues.Kilocalories);
        Assert.Equal(348m, second.Quantity);
    }

    [Fact]
    public void Create_RepeatedFoodInSameMeal_ProducesIndependentEntries()
    {
        var food = CreateFood();

        var first = DiaryEntry.Create(
                food,
                Date,
                MealType.Breakfast,
                100m,
                ReferenceUnit.Gram
            )
            .Value;
        var second = DiaryEntry.Create(
                food,
                Date,
                MealType.Breakfast,
                100m,
                ReferenceUnit.Gram
            )
            .Value;

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first, second);
        Assert.Equal(first.FoodSnapshot, second.FoodSnapshot);
    }

    [Fact]
    public void Factories_WithMissingFood_ReturnDomainError()
    {
        Assert.Same(
            DiaryEntryErrors.FoodRequired,
            DiaryEntry.Create(null, Date, MealType.Lunch, 1m, ReferenceUnit.Gram).Error
        );
        Assert.Same(
            DiaryEntryErrors.FoodRequired,
            DiaryEntry.CreateFromServing(
                    null,
                    Date,
                    MealType.Lunch,
                    ServingSizeId.New(),
                    1m
                )
                .Error
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)]
    public void Factories_WithUndefinedMeal_ReturnDomainError(int value)
    {
        var serving = ServingSize.Create(1m, "piece", null, 1m).Value;
        var food = CreateFood(servings: [serving]);

        Assert.Same(
            DiaryEntryErrors.InvalidMealType,
            DiaryEntry.Create(food, Date, (MealType)value, 1m, ReferenceUnit.Gram).Error
        );
        Assert.Same(
            DiaryEntryErrors.InvalidMealType,
            DiaryEntry.CreateFromServing(
                    food,
                    Date,
                    (MealType)value,
                    serving.Id,
                    1m
                )
                .Error
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonpositiveQuantity_ReturnsDomainError(int quantity)
    {
        var result = DiaryEntry.Create(
            CreateFood(),
            Date,
            MealType.Lunch,
            quantity,
            ReferenceUnit.Gram
        );

        Assert.Same(DiaryEntryErrors.InvalidQuantity, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    public void Create_WithUndefinedUnit_ReturnsDomainError(int unit)
    {
        var result = DiaryEntry.Create(
            CreateFood(),
            Date,
            MealType.Lunch,
            1m,
            (ReferenceUnit)unit
        );

        Assert.Same(DiaryEntryErrors.InvalidReferenceUnit, result.Error);
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram, ReferenceUnit.Milliliter)]
    [InlineData(ReferenceUnit.Milliliter, ReferenceUnit.Gram)]
    public void Create_WithMismatchedUnit_ReturnsDomainError(
        ReferenceUnit foodUnit,
        ReferenceUnit consumedUnit
    )
    {
        var result = DiaryEntry.Create(
            CreateFood(foodUnit),
            Date,
            MealType.Lunch,
            1m,
            consumedUnit
        );

        Assert.Same(DiaryEntryErrors.ReferenceUnitMismatch, result.Error);
    }

    [Fact]
    public void CreateFromServing_WithMissingServingId_ReturnsDomainError()
    {
        var result = DiaryEntry.CreateFromServing(
            CreateFood(),
            Date,
            MealType.Lunch,
            null,
            1m
        );

        Assert.Same(DiaryEntryErrors.ServingSizeIdRequired, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateFromServing_WithNonpositiveCount_ReturnsDomainError(int count)
    {
        var serving = ServingSize.Create(1m, "piece", null, 1m).Value;

        var result = DiaryEntry.CreateFromServing(
            CreateFood(servings: [serving]),
            Date,
            MealType.Lunch,
            serving.Id,
            count
        );

        Assert.Same(DiaryEntryErrors.InvalidServingCount, result.Error);
    }

    [Fact]
    public void CreateFromServing_WithForeignServing_ReturnsDomainError()
    {
        var ownServing = ServingSize.Create(1m, "piece", "small", 1m).Value;
        var foreignServing = ServingSize.Create(1m, "piece", "large", 2m).Value;
        var food = CreateFood(servings: [ownServing]);
        _ = CreateFood(servings: [foreignServing]);

        var result = DiaryEntry.CreateFromServing(
            food,
            Date,
            MealType.Lunch,
            foreignServing.Id,
            1m
        );

        Assert.Same(DiaryEntryErrors.ServingSizeNotFound, result.Error);
    }

    [Fact]
    public void CreateFromServing_WithFoodWithoutServings_ReturnsDomainError()
    {
        var result = DiaryEntry.CreateFromServing(
            CreateFood(),
            Date,
            MealType.Lunch,
            ServingSizeId.New(),
            1m
        );

        Assert.Same(DiaryEntryErrors.ServingSizeNotFound, result.Error);
    }

    [Fact]
    public void CreateFromServing_WithOverflowingQuantity_ReturnsDomainError()
    {
        var serving = ServingSize.Create(1m, "portion", null, decimal.MaxValue).Value;
        var food = CreateFood(
            nutrition: NutritionalValues.Create(0m, 0m, 0m, 0m).Value,
            servings: [serving]
        );

        var result = DiaryEntry.CreateFromServing(
            food,
            Date,
            MealType.Lunch,
            serving.Id,
            2m
        );

        Assert.Same(DiaryEntryErrors.QuantityOutOfRange, result.Error);
    }

    [Fact]
    public void CreateFromServing_WithQuantityUnderflow_ReturnsDomainError()
    {
        var serving = ServingSize.Create(
                1m,
                "portion",
                null,
                0.0000000000000000000000000001m
            )
            .Value;
        var food = CreateFood(servings: [serving]);

        var result = DiaryEntry.CreateFromServing(
            food,
            Date,
            MealType.Lunch,
            serving.Id,
            0.1m
        );

        Assert.Same(DiaryEntryErrors.QuantityOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Factories_WithOverflowingNutrient_ReturnDomainError(int nutrient)
    {
        var values = new decimal[4];
        values[nutrient] = decimal.MaxValue;
        var nutrition = NutritionalValues.Create(
                values[0],
                values[1],
                values[2],
                values[3]
            )
            .Value;
        var serving = ServingSize.Create(1m, "piece", null, 1m).Value;
        var food = CreateFood(basis: 1m, nutrition: nutrition, servings: [serving]);

        Assert.Same(
            DiaryEntryErrors.NutritionOutOfRange,
            DiaryEntry.Create(food, Date, MealType.Lunch, 2m, ReferenceUnit.Gram).Error
        );
        Assert.Same(
            DiaryEntryErrors.NutritionOutOfRange,
            DiaryEntry.CreateFromServing(
                    food,
                    Date,
                    MealType.Lunch,
                    serving.Id,
                    2m
                )
                .Error
        );
    }

    [Fact]
    public void Create_WithLargeQuantityAndZeroNutrition_Succeeds()
    {
        var nutrition = NutritionalValues.Create(0m, 0m, 0m, 0m).Value;
        var food = CreateFood(nutrition: nutrition);

        var entry = DiaryEntry.Create(
                food,
                Date,
                MealType.Lunch,
                decimal.MaxValue,
                ReferenceUnit.Gram
            )
            .Value;

        Assert.Equal(decimal.MaxValue, entry.Quantity);
        Assert.Equal(nutrition, entry.NutritionalValues);
    }

    private static Food CreateFood(
        ReferenceUnit unit = ReferenceUnit.Gram,
        decimal basis = 100m,
        NutritionalValues? nutrition = null,
        IReadOnlyList<ServingSize?>? servings = null
    )
    {
        return Food.Create(
                "Egg",
                null,
                basis,
                unit,
                nutrition ?? NutritionalValues.Create(150m, 10m, 1m, 5m).Value,
                servings
            )
            .Value;
    }
}
