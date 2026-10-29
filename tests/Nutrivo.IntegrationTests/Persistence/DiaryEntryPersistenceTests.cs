using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Application.Diaries;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure;
using Nutrivo.Infrastructure.Persistence;

namespace Nutrivo.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class DiaryEntryPersistenceTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(ReferenceUnit.Gram, false)]
    [InlineData(ReferenceUnit.Milliliter, false)]
    [InlineData(ReferenceUnit.Gram, true)]
    [InlineData(ReferenceUnit.Milliliter, true)]
    public async Task Repository_CommitsEntryAndRoundTripsSnapshotsAndPrecision(
        ReferenceUnit unit,
        bool serving
    )
    {
        var options = await CreateDatabaseAsync();
        var food = CreateFood(unit);
        await using (var context = new NutrivoDbContext(options))
        {
            context.Foods.Add(food);
            await context.SaveChangesAsync();
        }

        await using var services = CreateServices(options);
        using var scope = services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IDiaryEntryRepository>();
        var entry = CreateEntry(food, serving);

        await writer.AddAsync(entry, CancellationToken.None);

        // A new context verifies that AddAsync committed before returning.
        await using var verification = new NutrivoDbContext(options);
        var stored = await verification.DiaryEntries.AsNoTracking().SingleAsync();
        AssertEntry(entry, stored);
        Assert.Equal(
            123.12345678901234567890123456m,
            stored.FoodSnapshot.NutritionalValues.Kilocalories
        );
        if (serving)
        {
            Assert.Equal(58m, stored.Quantity);
            Assert.Equal(0.5m, stored.ServingCount);
            Assert.Equal(
                food.ServingSizes.Single().Id,
                stored.ServingSizeSnapshot!.ServingSizeId
            );
        }
        else
        {
            Assert.Null(stored.ServingSizeSnapshot);
            Assert.Null(stored.ServingCount);
        }
    }

    [Fact]
    public async Task Snapshots_RemainUnchangedAfterCatalogueUpdateAndDeletion()
    {
        var options = await CreateDatabaseAsync();
        var food = CreateFood(ReferenceUnit.Gram);
        var direct = CreateEntry(food, false);
        var serving = CreateEntry(food, true);
        await using (var context = new NutrivoDbContext(options))
        {
            context.Foods.Add(food);
            context.DiaryEntries.AddRange(direct, serving);
            await context.SaveChangesAsync();
        }

        await using (var context = new NutrivoDbContext(options))
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE public.foods SET name = 'Changed', brand = NULL, reference_quantity = 200,
                    kilocalories = 999, protein_grams = 0, carbohydrates_grams = 0, fat_grams = 0
                WHERE id = {food.Id.Value}
                """
            );
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE public.food_serving_sizes SET quantity = 3, unit = 'Changed', label = NULL,
                    equivalent_quantity = 999 WHERE food_id = {food.Id.Value}
                """
            );
        }

        await AssertStoredEntriesAsync(options, direct, serving);
        await using (var context = new NutrivoDbContext(options))
        {
            await context.Foods.ExecuteDeleteAsync();
        }

        await AssertStoredEntriesAsync(options, direct, serving);
    }

    [Fact]
    public async Task
        FoodRepository_LoadsServingsWithoutTrackingAndReturnsNullWhenMissing()
    {
        var options = await CreateDatabaseAsync();
        var food = CreateFood(ReferenceUnit.Gram);
        await using (var context = new NutrivoDbContext(options))
        {
            context.Foods.Add(food);
            await context.SaveChangesAsync();
        }

        await using var services = CreateServices(options);
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var contextFromScope =
            scope.ServiceProvider.GetRequiredService<NutrivoDbContext>();

        var loaded = await repository.GetByIdAsync(food.Id, CancellationToken.None);
        var missing = await repository.GetByIdAsync(FoodId.New(), CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(food.Id, loaded.Id);
        Assert.Equal(
            food.ServingSizes.Single().Id,
            Assert.Single(loaded.ServingSizes).Id
        );
        Assert.Equal(food.NutritionalValues, loaded.NutritionalValues);
        Assert.Null(missing);
        Assert.Empty(contextFromScope.ChangeTracker.Entries());
    }

    private static Food CreateFood(ReferenceUnit unit)
    {
        return Food.Create(
                "Original",
                "Brand",
                100m,
                unit,
                NutritionalValues.Create(
                        123.12345678901234567890123456m,
                        1.005m,
                        2.345m,
                        3.456m
                    )
                    .Value,
                [ServingSize.Create(2m, "pieces", "Medium", 116m).Value]
            )
            .Value;
    }

    private static DiaryEntry CreateEntry(Food food, bool serving)
    {
        return serving
            ? DiaryEntry.CreateFromServing(
                    food,
                    new DateOnly(2030, 1, 1),
                    MealType.Dinner,
                    food.ServingSizes.Single().Id,
                    0.5m
                )
                .Value
            : DiaryEntry.Create(
                    food,
                    new DateOnly(2020, 2, 29),
                    MealType.Snacks,
                    33.333m,
                    food.ReferenceUnit
                )
                .Value;
    }

    private static void AssertEntry(DiaryEntry expected, DiaryEntry actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Date, actual.Date);
        Assert.Equal(expected.MealType, actual.MealType);
        Assert.Equal(expected.Quantity, actual.Quantity);
        Assert.Equal(expected.ReferenceUnit, actual.ReferenceUnit);
        Assert.Equal(expected.ServingCount, actual.ServingCount);
        Assert.Equal(expected.FoodSnapshot, actual.FoodSnapshot);
        Assert.Equal(expected.ServingSizeSnapshot, actual.ServingSizeSnapshot);
        Assert.Equal(expected.NutritionalValues, actual.NutritionalValues);
    }

    private static async Task AssertStoredEntriesAsync(
        DbContextOptions<NutrivoDbContext> options,
        params DiaryEntry[] expected
    )
    {
        await using var context = new NutrivoDbContext(options);
        var entries = await context.DiaryEntries.AsNoTracking().ToListAsync();
        Assert.Equal(expected.Length, entries.Count);
        foreach (var entry in expected)
        {
            AssertEntry(entry, entries.Single(stored => stored.Id == entry.Id));
        }
    }

    private static ServiceProvider CreateServices(
        DbContextOptions<NutrivoDbContext> options
    )
    {
        using var context = new NutrivoDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Nutrivo"] = context.Database.GetConnectionString()
                }
            )
            .Build();
        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(
                new ServiceProviderOptions
                    { ValidateScopes = true, ValidateOnBuild = true }
            );
    }

    private async Task<DbContextOptions<NutrivoDbContext>> CreateDatabaseAsync()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        return options;
    }
}
