using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence.Repositories;

internal sealed class FoodRepository(NutrivoDbContext context) : IFoodRepository
{
    public async Task<bool> DeleteAsync(FoodId id, CancellationToken cancellationToken)
    {
        return await context
                .Foods.Where(food => food.Id == id)
                .ExecuteDeleteAsync(cancellationToken)
            == 1;
    }

    public Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken)
    {
        return context
            .Foods.AsNoTracking()
            .SingleOrDefaultAsync(
                food => food.Id == id,
                cancellationToken
            );
    }

    public async Task AddAsync(Food food, CancellationToken cancellationToken)
    {
        context.Foods.Add(food);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Food?> GetForUpdateAsync(
        FoodId id,
        CancellationToken cancellationToken
    )
    {
        if (context.Database.CurrentTransaction is null)
        {
            await context.Database.BeginTransactionAsync(cancellationToken);
        }

        // Serialize catalogue writes before reading the serving collection. A second
        // statement sees the latest servings after any preceding writer commits.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM public.foods WHERE id = {id.Value} FOR UPDATE",
            cancellationToken
        );
        return await context.Foods.SingleOrDefaultAsync(
            food => food.Id == id,
            cancellationToken
        );
    }

    public async Task SaveAsync(Food food, CancellationToken cancellationToken)
    {
        context.ChangeTracker.DetectChanges();
        await using var transaction = context.Database.CurrentTransaction
            ?? await context.Database.BeginTransactionAsync(cancellationToken);

        // Free every persisted position before EF writes the replacement order.
        // Negative positions are private to this transaction and never committed.
        await context
            .Set<ServingSize>()
            .Where(serving => EF.Property<FoodId>(serving, "FoodId") == food.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    serving => serving.Position,
                    serving => -serving.Position - 1
                ),
                cancellationToken
            );

        foreach (var entry in context.ChangeTracker.Entries<ServingSize>())
        {
            if (entry.State is EntityState.Added or EntityState.Detached
                || entry.Property<FoodId>("FoodId").OriginalValue != food.Id)
            {
                continue;
            }

            var position = entry.Property(serving => serving.Position);
            position.OriginalValue = -position.OriginalValue - 1;
            if (entry.State != EntityState.Deleted)
            {
                position.IsModified = true;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
