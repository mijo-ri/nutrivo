using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.Domain.Foods;
using Npgsql;

namespace Nutrivo.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class MigrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Migrations_CanApplyReapplyRollbackAndRebuild()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);

        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());

        await context.Database.MigrateAsync();

        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(2, applied.Length);
        Assert.EndsWith("_FoodServingSizes", applied[^1]);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await context.Foods.CountAsync());

        await context.Database.MigrateAsync();

        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(Migration.InitialDatabase);

        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(2, (await context.Database.GetPendingMigrationsAsync()).Count());
        Assert.True(
            await context
                .Database.SqlQuery<bool>(
                    $"SELECT to_regclass('public.foods') IS NULL AS \"Value\""
                )
                .SingleAsync()
        );

        await context.Database.MigrateAsync();

        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(0, await context.Foods.CountAsync());
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
