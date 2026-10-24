using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nutrivo.Infrastructure.Persistence;

namespace Nutrivo.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class MigrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task InitialCreate_CanApplyReapplyRollbackAndRebuild()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);

        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());

        await context.Database.MigrateAsync();

        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.EndsWith("_InitialCreate", Assert.Single(applied));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await context.Foods.CountAsync());

        await context.Database.MigrateAsync();

        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(Migration.InitialDatabase);

        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        Assert.Single(await context.Database.GetPendingMigrationsAsync());
        Assert.True(await context.Database.SqlQuery<bool>(
            $"SELECT to_regclass('public.foods') IS NULL AS \"Value\""
        ).SingleAsync());

        await context.Database.MigrateAsync();

        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(0, await context.Foods.CountAsync());
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
