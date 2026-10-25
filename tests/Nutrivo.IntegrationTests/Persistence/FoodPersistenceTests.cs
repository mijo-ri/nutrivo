using Microsoft.EntityFrameworkCore;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;

namespace Nutrivo.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class FoodPersistenceTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(ReferenceUnit.Gram, null)]
    [InlineData(ReferenceUnit.Gram, "Example")]
    [InlineData(ReferenceUnit.Milliliter, null)]
    [InlineData(ReferenceUnit.Milliliter, "Example")]
    public async Task SaveAndReload_PreservesFoodAndTypedIdentity(
        ReferenceUnit unit,
        string? brand
    )
    {
        var options = await fixture.CreateDatabaseAsync();
        var nutrition = NutritionalValues.Create(372m, 13.5m, 58.7m, 7m).Value;
        var food = Food.Create("Rolled  oats", brand, unit, nutrition).Value;

        await using (var context = new NutrivoDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.Foods.Add(food);
            await context.SaveChangesAsync();
        }

        await using var readContext = new NutrivoDbContext(options);
        var id = FoodId.Create(food.Id.Value).Value;
        var loaded = await readContext.Foods.SingleAsync(candidate => candidate.Id == id);

        Assert.NotSame(food, loaded);
        Assert.Equal(food.Id, loaded.Id);
        Assert.Equal(food.Name, loaded.Name);
        Assert.Equal(brand, loaded.Brand);
        Assert.Equal(unit, loaded.ReferenceUnit);
        Assert.Equal(100m, loaded.ReferenceQuantity);
        Assert.Equal(nutrition, loaded.NutritionalValues);
    }

    public static TheoryData<decimal, decimal, decimal, decimal> NutritionValues => new()
    {
        { 372.123456789m, 13.567891234m, 58.765432198m, 7.012345678m },
        { 0m, 0m, 0m, 0m },
        { decimal.MaxValue, decimal.MaxValue, decimal.MaxValue, decimal.MaxValue },
        { 0.0000000000000000000000000001m, 1.2345678901234567890123456789m, 1m, 2m }
    };

    [Theory]
    [MemberData(nameof(NutritionValues))]
    public async Task SaveAndReload_PreservesDecimalsWithoutRounding(
        decimal kilocalories,
        decimal protein,
        decimal carbohydrates,
        decimal fat
    )
    {
        var options = await fixture.CreateDatabaseAsync();
        var nutrition = NutritionalValues.Create(
                kilocalories,
                protein,
                carbohydrates,
                fat
            )
            .Value;
        var food = Food.Create("Product", null, ReferenceUnit.Gram, nutrition).Value;

        await using (var context = new NutrivoDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.Foods.Add(food);
            await context.SaveChangesAsync();
        }

        await using var readContext = new NutrivoDbContext(options);
        var loaded = await readContext.Foods.AsNoTracking().SingleAsync();

        Assert.Equal(kilocalories, loaded.NutritionalValues.Kilocalories);
        Assert.Equal(protein, loaded.NutritionalValues.ProteinGrams);
        Assert.Equal(carbohydrates, loaded.NutritionalValues.CarbohydratesGrams);
        Assert.Equal(fat, loaded.NutritionalValues.FatGrams);
    }

    [Fact]
    public async Task SaveAndReload_WithSharedNutrition_PersistsIndependentFoods()
    {
        var options = await fixture.CreateDatabaseAsync();
        var nutrition = NutritionalValues.Create(100m, 1m, 2m, 3m).Value;
        var first = Food.Create("Product", null, ReferenceUnit.Gram, nutrition).Value;
        var second = Food.Create("Product", null, ReferenceUnit.Gram, nutrition).Value;

        await using (var context = new NutrivoDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.Foods.AddRange(first, second);
            await context.SaveChangesAsync();
        }

        await using var readContext = new NutrivoDbContext(options);
        var foods = await readContext.Foods.ToListAsync();

        Assert.Equal(2, foods.Count);
        Assert.Contains(foods, food => food.Id == first.Id);
        Assert.Contains(foods, food => food.Id == second.Id);
        Assert.All(foods, food => Assert.Equal(nutrition, food.NutritionalValues));
    }

    [Fact]
    public async Task SaveAndReload_AcceptsMaximumNameAndBrandLengths()
    {
        var options = await fixture.CreateDatabaseAsync();
        var food = Food.Create(
                new string('ä', 200),
                new string('ö', 200),
                ReferenceUnit.Gram,
                NutritionalValues.Create(0m, 0m, 0m, 0m).Value
            )
            .Value;

        await using (var context = new NutrivoDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.Foods.Add(food);
            await context.SaveChangesAsync();
        }

        await using var readContext = new NutrivoDbContext(options);
        var loaded = await readContext.Foods.SingleAsync();

        Assert.Equal(food.Name, loaded.Name);
        Assert.Equal(food.Brand, loaded.Brand);
    }
}
