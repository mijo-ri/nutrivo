using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Foods;

public sealed class FoodUpdateTests
{
    [Fact]
    public void Update_ReplacesAllFieldsAndServingsWhilePreservingExistingIdentities()
    {
        var food = CreateFood();
        var id = food.Id;
        var first = food.ServingSizes[0];
        var second = food.ServingSizes[1];
        var removed = food.ServingSizes[2];
        var nutrition = NutritionalValues.Create(123.123456789m, 4m, 5m, 6m).Value;

        var result = food.Update(
            "  New name  ",
            "  Brand  ",
            50m,
            ReferenceUnit.Milliliter,
            nutrition,
            [Input(second.Id, 25m), Input(null, 100m), Input(first.Id, 75m)]
        );

        Assert.True(result.IsSuccess);
        Assert.Same(food, result.Value);
        Assert.Equal(id, food.Id);
        Assert.Equal("New name", food.Name);
        Assert.Equal("Brand", food.Brand);
        Assert.Equal(50m, food.ReferenceQuantity);
        Assert.Equal(ReferenceUnit.Milliliter, food.ReferenceUnit);
        Assert.Equal(nutrition, food.NutritionalValues);
        Assert.Same(second, food.ServingSizes[0]);
        Assert.Same(first, food.ServingSizes[2]);
        Assert.DoesNotContain(food.ServingSizes, serving => serving.Id == removed.Id);
        Assert.DoesNotContain(
            food.ServingSizes[1].Id,
            new[]
            {
                first.Id, second.Id, removed.Id
            }
        );
        Assert.Equal(
            [
                0, 1, 2
            ],
            food.ServingSizes.Select(serving => serving.Position)
        );
        Assert.Equal(25m, second.EquivalentQuantity);
        Assert.Equal(2m, second.Quantity);
        Assert.Equal("Becher", second.Unit);
        Assert.Equal("Gross", second.Label);
        Assert.Equal(61.5617283945m, food.CalculateNutrition(second).Value.Kilocalories);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Update_WithNullOrEmptyServings_RemovesAllAndClearsBlankBrand(bool empty)
    {
        var food = CreateFood();
        var result = food.Update(
            "Updated",
            "  ",
            100m,
            ReferenceUnit.Gram,
            food.NutritionalValues,
            empty ? [] : null
        );
        Assert.True(result.IsSuccess);
        Assert.Null(food.Brand);
        Assert.Empty(food.ServingSizes);
    }

    public static
        TheoryData<string?, string?, decimal, ReferenceUnit, NutritionalValues?,
            DomainError> InvalidFields
    {
        get
        {
            return new TheoryData<string?, string?, decimal, ReferenceUnit, NutritionalValues?, DomainError>
            {
                {
                    null, null, 100m, ReferenceUnit.Gram, Nutrition(),
                    FoodErrors.NameRequired
                },
                {
                    new string('x', 201), null, 100m, ReferenceUnit.Gram, Nutrition(),
                    FoodErrors.NameTooLong
                },
                {
                    "New", new string('x', 201), 100m, ReferenceUnit.Gram, Nutrition(),
                    FoodErrors.BrandTooLong
                },
                {
                    "New", null, 0m, ReferenceUnit.Gram, Nutrition(),
                    FoodErrors.InvalidReferenceQuantity
                },
                {
                    "New", null, -1m, ReferenceUnit.Gram, Nutrition(),
                    FoodErrors.InvalidReferenceQuantity
                },
                {
                    "New", null, 100m, (ReferenceUnit)0, Nutrition(),
                    FoodErrors.InvalidReferenceUnit
                },
                {
                    "New", null, 100m, ReferenceUnit.Gram, null,
                    FoodErrors.NutritionalValuesRequired
                }
            };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidFields))]
    public void Update_WithInvalidFields_DoesNotMutate(
        string? name,
        string? brand,
        decimal quantity,
        ReferenceUnit unit,
        NutritionalValues? nutrition,
        DomainError error
    )
    {
        var food = CreateFood();
        var before = State(food);
        var result = food.Update(
            name,
            brand,
            quantity,
            unit,
            nutrition,
            [Input(food.ServingSizes[0].Id, 999m)]
        );
        Assert.Equal(error, result.Error);
        Assert.Equal(before, State(food));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("foreign")]
    [InlineData("duplicate")]
    [InlineData("quantity")]
    [InlineData("unit")]
    [InlineData("unitLength")]
    [InlineData("labelLength")]
    [InlineData("equivalent")]
    [InlineData("overflow")]
    public void Update_WithInvalidServing_DoesNotPartiallyMutate(string scenario)
    {
        var food = CreateFood();
        var before = State(food);
        var first = Input(food.ServingSizes[0].Id, 50m);
        ServingSizeUpdate? invalid = scenario switch
        {
            "null" => null,
            "foreign" => Input(ServingSizeId.New(), 1m),
            "duplicate" => first,
            "quantity" => Input(null, 1m) with
            {
                Quantity = 0m
            },
            "unit" => Input(null, 1m) with
            {
                Unit = " "
            },
            "unitLength" => Input(null, 1m) with
            {
                Unit = new string('x', 201)
            },
            "labelLength" => Input(null, 1m) with
            {
                Label = new string('x', 201)
            },
            "equivalent" => Input(null, 0m),
            _ => Input(null, decimal.MaxValue)
        };
        var result = food.Update(
            "Changed",
            null,
            1m,
            ReferenceUnit.Milliliter,
            Nutrition(),
            [first, invalid]
        );
        Assert.True(result.IsFailure);
        Assert.Equal(before, State(food));
    }

    [Fact]
    public void Update_DoesNotChangePreviouslyCapturedDiarySnapshots()
    {
        var food = CreateFood();
        var entry = DiaryEntry.CreateFromServing(
                food,
                new DateOnly(2026, 11, 8),
                MealType.Breakfast,
                food.ServingSizes[0].Id,
                2m
            )
            .Value;
        var originalNutrition = entry.NutritionalValues;
        var originalFoodNutrition = entry.FoodSnapshot.NutritionalValues;
        var originalServing = entry.ServingSizeSnapshot!;

        food.Update(
            "New",
            null,
            50m,
            ReferenceUnit.Milliliter,
            NutritionalValues.Create(900m, 0m, 0m, 0m).Value,
            [Input(food.ServingSizes[0].Id, 999m)]
        );

        Assert.Equal("Original", entry.FoodSnapshot.Name);
        Assert.Equal(ReferenceUnit.Gram, entry.ReferenceUnit);
        Assert.Equal(100m, entry.FoodSnapshot.ReferenceQuantity);
        Assert.Equal(originalFoodNutrition, entry.FoodSnapshot.NutritionalValues);
        Assert.Equal(originalNutrition, entry.NutritionalValues);
        Assert.Equal(10m, originalServing.EquivalentQuantity);
        Assert.Equal("Spoon", originalServing.Unit);
    }

    private static NutritionalValues Nutrition()
    {
        return NutritionalValues.Create(100m, 1m, 2m, 3m).Value;
    }

    private static Food CreateFood()
    {
        return Food.Create(
                "Original",
                "Original brand",
                100m,
                ReferenceUnit.Gram,
                Nutrition(),
                [
                    ServingSize.Create(1m, "Spoon", null, 10m).Value,
                    ServingSize.Create(1m, "Cup", null, 20m).Value,
                    ServingSize.Create(1m, "Bowl", null, 30m).Value
                ]
            )
            .Value;
    }

    private static ServingSizeUpdate Input(ServingSizeId? id, decimal equivalent)
    {
        return new ServingSizeUpdate(id, 2m, " Becher ", " Gross ", equivalent);
    }

    private static string State(Food food)
    {
        return System.Text.Json.JsonSerializer.Serialize(food);
    }
}
