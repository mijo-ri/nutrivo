using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Foods;

namespace Nutrivo.IntegrationTests.Persistence;

[Collection("PostgreSQL")]
public sealed class DeleteFoodPersistenceTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_CommitsCascadeAndPreservesOtherFoods(bool servings)
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture, servings);
        await using var factory = new FoodApiFactory(options);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
        Assert.False(await repository.DeleteAsync(FoodId.New(), CancellationToken.None));
        Assert.True(await repository.DeleteAsync(food.Id, CancellationToken.None));
        Assert.False(await repository.DeleteAsync(food.Id, CancellationToken.None));
        Assert.Empty(
            scope
                .ServiceProvider.GetRequiredService<NutrivoDbContext>()
                .ChangeTracker.Entries()
        );
        await using var read = new NutrivoDbContext(options);
        Assert.Equal("Unrelated", (await read.Foods.SingleAsync()).Name);
        Assert.Equal(2, await read.Set<ServingSize>().CountAsync());
        Assert.Equal(servings ? 6 : 0, await read.DiaryEntries.CountAsync());
    }

    [Fact]
    public async Task Delete_CancellationLeavesDataUnchanged()
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture);
        var before = await DeleteFoodTestData.StateAsync(options);
        await using var factory = new FoodApiFactory(options);
        using var scope = factory.Services.CreateScope();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope
                .ServiceProvider.GetRequiredService<IFoodRepository>()
                .DeleteAsync(food.Id, cancellation.Token)
        );
        Assert.Equal(before, await DeleteFoodTestData.StateAsync(options));
    }

    [Fact]
    public async Task Delete_ConcurrentRequestsHaveOneWinner()
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture);
        await using var factory = new FoodApiFactory(options);
        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();
        var results = await Task.WhenAll(
            first
                .ServiceProvider.GetRequiredService<IFoodRepository>()
                .DeleteAsync(food.Id, CancellationToken.None),
            second
                .ServiceProvider.GetRequiredService<IFoodRepository>()
                .DeleteAsync(food.Id, CancellationToken.None)
        );
        Assert.Single(results, result => result);
        await using var read = new NutrivoDbContext(options);
        Assert.False(await read.Foods.AnyAsync(f => f.Id == food.Id));
        Assert.Equal(6, await read.DiaryEntries.CountAsync());
    }

    [Fact]
    public async Task Delete_RespectsUpdateLockAndDoesNotResurrectFood()
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture);
        await using var factory = new FoodApiFactory(options);
        using var writer = factory.Services.CreateScope();
        var updates = writer.ServiceProvider.GetRequiredService<IFoodRepository>();
        var loaded = (await updates.GetForUpdateAsync(food.Id, CancellationToken.None))!;
        // The held row lock forces the delete to wait; cancellation bounds that wait.
        using (var blocked = factory.Services.CreateScope())
        using (var cancellation =
               new CancellationTokenSource(TimeSpan.FromMilliseconds(500)))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                blocked
                    .ServiceProvider.GetRequiredService<IFoodRepository>()
                    .DeleteAsync(food.Id, cancellation.Token)
            );
        }

        Assert.True(
            loaded.Update(
                    "Updated",
                    null,
                    100m,
                    ReferenceUnit.Gram,
                    loaded.NutritionalValues
                )
                .IsSuccess
        );
        await updates.SaveAsync(loaded, CancellationToken.None);
        using var deletion = factory.Services.CreateScope();
        Assert.True(
            await deletion
                .ServiceProvider.GetRequiredService<IFoodRepository>()
                .DeleteAsync(food.Id, CancellationToken.None)
        );
        using var laterUpdate = factory.Services.CreateScope();
        Assert.Null(
            await laterUpdate
                .ServiceProvider.GetRequiredService<IFoodRepository>()
                .GetForUpdateAsync(food.Id, CancellationToken.None)
        );
        await using var read = new NutrivoDbContext(options);
        Assert.False(await read.Foods.AnyAsync(f => f.Id == food.Id));
    }

    [Fact]
    public async Task Delete_FailedCascadeRollsBackEntireStatement()
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture);
        var before = await DeleteFoodTestData.StateAsync(options);
        await DeleteFoodTestData.RejectServingDeletionAsync(options);
        await using var factory = new FoodApiFactory(options);
        using var scope = factory.Services.CreateScope();
        await Assert.ThrowsAsync<PostgresException>(() => scope
            .ServiceProvider
            .GetRequiredService<IFoodRepository>()
            .DeleteAsync(food.Id, CancellationToken.None)
        );
        Assert.Equal(before, await DeleteFoodTestData.StateAsync(options));
    }
}
