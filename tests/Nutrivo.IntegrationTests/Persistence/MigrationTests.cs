using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.Domain.Foods;
using Nutrivo.Domain.Diaries;
using Npgsql;

namespace Nutrivo.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class MigrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task
        DiaryEntryCaptureTime_BackfillsLegacyEntriesAndPreservesDataOnRollback()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        var food = Food.Create(
                "Legacy",
                "Brand",
                100m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(150.005m, 10.005m, 1.005m, 5.005m).Value,
                [ServingSize.Create(2m, "pieces", "Medium", 116m).Value]
            )
            .Value;
        var date = new DateOnly(2020, 2, 29);
        var direct = DiaryEntry.Create(
                food,
                date,
                MealType.Breakfast,
                25.123m,
                ReferenceUnit.Gram
            )
            .Value;
        var serving = DiaryEntry.CreateFromServing(
                food,
                date,
                MealType.Lunch,
                food.ServingSizes.Single().Id,
                0.5m
            )
            .Value;
        context.Foods.Add(food);
        context.DiaryEntries.AddRange(direct, serving);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var migrator = context.GetService<IMigrator>();
        var legacyMigration = context
            .Database.GetMigrations()
            .Single(migration => migration.EndsWith("_DiaryEntries"));
        await migrator.MigrateAsync(legacyMigration);
        var before = await DatabaseTimeAsync();

        await context.Database.MigrateAsync();

        var after = await DatabaseTimeAsync();
        var timestamps = await context
            .DiaryEntries.AsNoTracking()
            .Select(entry => EF.Property<DateTimeOffset>(entry, "CreatedAt"))
            .ToArrayAsync();
        Assert.Equal(2, timestamps.Length);
        Assert.Equal(timestamps[0], timestamps[1]);
        Assert.All(timestamps, time => Assert.InRange(time, before, after));
        await AssertEntriesUnchangedAsync(direct, serving);

        var added = DiaryEntry.Create(
                food,
                date,
                MealType.Snacks,
                50m,
                ReferenceUnit.Gram
            )
            .Value;
        var beforeInsert = await DatabaseTimeAsync();
        context.DiaryEntries.Add(added);
        await context.SaveChangesAsync();
        var afterInsert = await DatabaseTimeAsync();
        var generated =
            context.Entry(added).Property<DateTimeOffset>("CreatedAt").CurrentValue;
        Assert.InRange(generated, beforeInsert, afterInsert);
        context.ChangeTracker.Clear();

        await migrator.MigrateAsync(legacyMigration);

        Assert.Equal(
            3L,
            await context
                .Database.SqlQuery<long>(
                    $"SELECT count(*) AS \"Value\" FROM public.diary_entries"
                )
                .SingleAsync()
        );
        Assert.False(
            await context
                .Database.SqlQuery<bool>(
                    $"""
                    SELECT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public' AND table_name = 'diary_entries'
                        AND column_name = 'created_at') AS "Value"
                    """
                )
                .SingleAsync()
        );
        await context.Database.MigrateAsync();
        await AssertEntriesUnchangedAsync(direct, serving, added);
        Assert.False(context.Database.HasPendingModelChanges());

        async Task<DateTimeOffset> DatabaseTimeAsync()
        {
            return await context
                .Database.SqlQuery<DateTimeOffset>(
                    $"SELECT CURRENT_TIMESTAMP AS \"Value\""
                )
                .SingleAsync();
        }

        async Task AssertEntriesUnchangedAsync(params DiaryEntry[] expected)
        {
            var stored = await context.DiaryEntries.AsNoTracking().ToArrayAsync();
            Assert.Equal(expected.Length, stored.Length);
            foreach (var original in expected)
            {
                var entry = Assert.Single(stored, entry => entry.Id == original.Id);
                Assert.Equal(original.Date, entry.Date);
                Assert.Equal(original.MealType, entry.MealType);
                Assert.Equal(original.Quantity, entry.Quantity);
                Assert.Equal(original.ReferenceUnit, entry.ReferenceUnit);
                Assert.Equal(original.FoodSnapshot, entry.FoodSnapshot);
                Assert.Equal(original.NutritionalValues, entry.NutritionalValues);
                Assert.Equal(original.ServingCount, entry.ServingCount);
                Assert.Equal(original.ServingSizeSnapshot, entry.ServingSizeSnapshot);
            }
        }
    }

    [Fact]
    public async Task Migrations_CanApplyReapplyRollbackAndRebuild()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);

        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());

        await context.Database.MigrateAsync();

        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(4, applied.Length);
        Assert.EndsWith("_DiaryEntryCaptureTime", applied[^1]);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await context.Foods.CountAsync());
        Assert.Equal(0, await context.DiaryEntries.CountAsync());

        await context.Database.MigrateAsync();

        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(Migration.InitialDatabase);

        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(4, (await context.Database.GetPendingMigrationsAsync()).Count());
        Assert.True(
            await context
                .Database.SqlQuery<bool>(
                    $"SELECT to_regclass('public.foods') IS NULL AND to_regclass('public.diary_entries') IS NULL AS \"Value\""
                )
                .SingleAsync()
        );

        await context.Database.MigrateAsync();

        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(0, await context.Foods.CountAsync());
        Assert.Equal(0, await context.DiaryEntries.CountAsync());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task
        FoodServingSizes_UpgradesLegacyFoodWithoutChangingItsIdentityOrNutrition()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        var migrator = context.GetService<IMigrator>();
        var initial = context.Database.GetMigrations().First();
        await migrator.MigrateAsync(initial);
        var id = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO public.foods
                (id, name, brand, reference_unit, kilocalories, protein_grams, carbohydrates_grams, fat_grams)
            VALUES ({id}, 'Legacy', 'Brand', 2, 123.12345678901234567890123456, 1.5, 2.5, 0)
            """
        );

        await context.Database.MigrateAsync();

        var food = await context.Foods.AsNoTracking().SingleAsync();
        Assert.Equal(id, food.Id.Value);
        Assert.Equal("Legacy", food.Name);
        Assert.Equal("Brand", food.Brand);
        Assert.Equal(ReferenceUnit.Milliliter, food.ReferenceUnit);
        Assert.Equal(100m, food.ReferenceQuantity);
        Assert.Equal(
            123.12345678901234567890123456m,
            food.NutritionalValues.Kilocalories
        );
        Assert.Equal(1.5m, food.NutritionalValues.ProteinGrams);
        Assert.Equal(2.5m, food.NutritionalValues.CarbohydratesGrams);
        Assert.Equal(0m, food.NutritionalValues.FatGrams);
        Assert.Empty(food.ServingSizes);

        // The old representation is still safe for an unchanged legacy food.
        await migrator.MigrateAsync(initial);
        await context.Database.MigrateAsync();
        Assert.Equal(id, (await context.Foods.AsNoTracking().SingleAsync()).Id.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FoodServingSizes_RejectsUnsafeRollbackAndPreservesData(
        bool withServing
    )
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        var food = Food.Create(
                "Egg",
                null,
                withServing ? 100m : 30m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(1m, 0m, 0m, 0m).Value,
                withServing ? [ServingSize.Create(1m, "piece", null, 58m).Value] : []
            )
            .Value;
        context.Foods.Add(food);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context
                .GetService<IMigrator>()
                .MigrateAsync(context.Database.GetMigrations().First())
        );

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        await using var verification = new NutrivoDbContext(options);
        Assert.Equal(
            2,
            (await verification.Database.GetAppliedMigrationsAsync()).Count()
        );
        var stored = await verification.Foods.SingleAsync();
        Assert.Equal(food.Id, stored.Id);
        Assert.Equal(food.ReferenceQuantity, stored.ReferenceQuantity);
        Assert.Equal(food.ServingSizes.Count, stored.ServingSizes.Count);
    }
}
