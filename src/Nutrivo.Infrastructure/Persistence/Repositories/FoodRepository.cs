using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence.Repositories;

internal sealed class FoodRepository(NutrivoDbContext context) : IFoodRepository
{
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
}
