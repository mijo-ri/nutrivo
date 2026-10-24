using Nutrivo.Application.Foods;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence.Repositories;

internal sealed class FoodRepository(NutrivoDbContext context) : IFoodRepository
{
    public async Task AddAsync(Food food, CancellationToken cancellationToken)
    {
        context.Foods.Add(food);
        await context.SaveChangesAsync(cancellationToken);
    }
}
