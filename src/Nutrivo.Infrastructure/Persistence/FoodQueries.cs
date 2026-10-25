using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.SearchFoods;

namespace Nutrivo.Infrastructure.Persistence;

internal sealed class FoodQueries(NutrivoDbContext context) : IFoodQueries
{
    public async Task<SearchFoodsResult> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var foods = context.Foods.AsNoTracking();

        if (search is not null)
        {
            var escaped = search
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            foods = foods.Where(food =>
                EF.Functions.ILike(food.Name, pattern, "\\")
                || (food.Brand != null && EF.Functions.ILike(food.Brand, pattern, "\\"))
            );
        }

        var totalCount = await foods.CountAsync(cancellationToken);
        var offset = checked((int)((page - 1L) * pageSize));
        var items = await foods
            .OrderBy(food => food.Name.ToLower())
            .ThenBy(food => (food.Brand ?? "").ToLower())
            .ThenBy(food => food.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(food => new FoodSearchItem(
                    food.Id.Value,
                    food.Name,
                    food.Brand,
                    100m,
                    food.ReferenceUnit,
                    food.NutritionalValues.Kilocalories,
                    food.NutritionalValues.ProteinGrams,
                    food.NutritionalValues.CarbohydratesGrams,
                    food.NutritionalValues.FatGrams
                )
            )
            .ToListAsync(cancellationToken);

        return new SearchFoodsResult(
            items.ToImmutableArray(),
            page,
            pageSize,
            totalCount
        );
    }
}
