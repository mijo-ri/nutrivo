using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Domain.Targets;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.Infrastructure.Persistence.Repositories;

namespace Nutrivo.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class DailyNutritionTargetPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Current_RoundTripsExactDecimalsAndReplacesWholeSet()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        var repository = new DailyNutritionTargetRepository(context);
        Assert.Null(await repository.GetCurrentAsync(default));
        var target = DailyNutritionTarget.Create(
                123.12345678901234567890123456m,
                decimal.MaxValue,
                0.0000000000000000000000000001m,
                0m
            )
            .Value;
        await repository.SetCurrentAsync(target, default);
        await repository.SetCurrentAsync(target, default);
        await using var fresh = new NutrivoDbContext(options);
        var stored =
            (await new DailyNutritionTargetRepository(fresh).GetCurrentAsync(default))!;
        Assert.Equal(target.Kilocalories, stored.Kilocalories);
        Assert.Equal(target.ProteinGrams, stored.ProteinGrams);
        Assert.Equal(target.CarbohydratesGrams, stored.CarbohydratesGrams);
        Assert.Equal(target.FatGrams, stored.FatGrams);
        await repository.SetCurrentAsync(
            DailyNutritionTarget.Create(0m, 0m, 0m, 0m).Value,
            default
        );
        stored =
            (await new DailyNutritionTargetRepository(fresh).GetCurrentAsync(default))!;
        Assert.Equal(0m, stored.Kilocalories);
        Assert.Equal(0m, stored.ProteinGrams);
        Assert.Equal(0m, stored.CarbohydratesGrams);
        Assert.Equal(0m, stored.FatGrams);
        Assert.Equal(1, await fresh.Set<DailyNutritionTarget>().CountAsync());
    }

    [Fact]
    public async Task Migration_UpgradesAndRollsBackWithoutChangingFoodOrDiaryData()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        var migrator =
            context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        var previous = context
            .Database.GetMigrations()
            .Single(name => name.EndsWith("_DiaryEntryCaptureTime"));
        await migrator.MigrateAsync(previous);
        var food = Nutrivo.Domain.Foods.Food.Create(
                "Existing",
                null,
                100m,
                Nutrivo.Domain.Foods.ReferenceUnit.Gram,
                Nutrivo.Domain.Foods.NutritionalValues.Create(123m, 1m, 2m, 3m).Value
            )
            .Value;
        context.Foods.Add(food);
        var entry = Nutrivo.Domain.Diaries.DiaryEntry.Create(
                food,
                new DateOnly(2026, 1, 1),
                Nutrivo.Domain.Diaries.MealType.Breakfast,
                50m,
                Nutrivo.Domain.Foods.ReferenceUnit.Gram
            )
            .Value;
        context.DiaryEntries.Add(entry);
        await context.SaveChangesAsync();
        await context.Database.MigrateAsync();
        Assert.Null(
            await new DailyNutritionTargetRepository(context).GetCurrentAsync(default)
        );
        await new DailyNutritionTargetRepository(context).SetCurrentAsync(
            DailyNutritionTarget.Create(1, 2, 3, 4).Value,
            default
        );
        await context.Database.MigrateAsync();
        Assert.Equal(1, await context.Set<DailyNutritionTarget>().CountAsync());
        await migrator.MigrateAsync(previous);
        await context.Database.MigrateAsync();
        Assert.Null(
            await new DailyNutritionTargetRepository(context).GetCurrentAsync(default)
        );
        context.ChangeTracker.Clear();
        Assert.Equal(food.Id, (await context.Foods.SingleAsync()).Id);
        var stored = await context.DiaryEntries.SingleAsync();
        Assert.Equal(entry.Id, stored.Id);
        Assert.Equal(entry.FoodSnapshot, stored.FoodSnapshot);
        Assert.Equal(entry.NutritionalValues, stored.NutritionalValues);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentSaves_LeaveOneCompleteSubmittedSet(bool existing)
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        if (existing)
        {
            await new DailyNutritionTargetRepository(context).SetCurrentAsync(
                DailyNutritionTarget.Create(0, 0, 0, 0).Value,
                default
            );
        }

        await Task.WhenAll(
            Enumerable
                .Range(1, 8)
                .Select(async index =>
                    {
                        await using var concurrent = new NutrivoDbContext(options);
                        await new DailyNutritionTargetRepository(concurrent)
                            .SetCurrentAsync(
                                DailyNutritionTarget.Create(
                                        index,
                                        index * 2,
                                        index * 3,
                                        index * 4
                                    )
                                    .Value,
                                default
                            );
                    }
                )
        );
        var stored =
            await context.Set<DailyNutritionTarget>().AsNoTracking().SingleAsync();
        Assert.InRange(stored.Kilocalories, 1m, 8m);
        Assert.Equal(stored.Kilocalories * 2, stored.ProteinGrams);
        Assert.Equal(stored.Kilocalories * 3, stored.CarbohydratesGrams);
        Assert.Equal(stored.Kilocalories * 4, stored.FatGrams);
    }
}
