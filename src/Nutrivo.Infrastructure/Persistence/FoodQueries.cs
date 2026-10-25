using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.SearchFoods;
using Nutrivo.Domain.Foods;

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
        var rows = await foods
            .OrderBy(food => food.Name.ToLower())
            .ThenBy(food => (food.Brand ?? "").ToLower())
            .ThenBy(food => food.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(food => new
                {
                    food.Id,
                    food.Name,
                    food.Brand,
                    food.ReferenceQuantity,
                    food.ReferenceUnit,
                    food.NutritionalValues,
                    ServingSizes = food
                        .ServingSizes.OrderBy(serving => serving.Position)
                        .ToList()
                }
            )
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(food => new FoodSearchItem(
                    food.Id.Value,
                    food.Name,
                    food.Brand,
                    food.ReferenceQuantity,
                    food.ReferenceUnit,
                    food.NutritionalValues.Kilocalories,
                    food.NutritionalValues.ProteinGrams,
                    food.NutritionalValues.CarbohydratesGrams,
                    food.NutritionalValues.FatGrams,
                    food
                        .ServingSizes.Select(serving => MapServing(
                                serving,
                                food.NutritionalValues,
                                food.ReferenceQuantity
                            )
                        )
                        .ToImmutableArray()
                )
            )
            .ToImmutableArray();

        return new SearchFoodsResult(
            items,
            page,
            pageSize,
            totalCount
        );
    }

    private static ServingSizeSearchItem MapServing(
        ServingSize serving,
        NutritionalValues basis,
        decimal referenceQuantity
    )
    {
        var nutrition = basis.Scale(serving.EquivalentQuantity, referenceQuantity).Value;
        return new ServingSizeSearchItem(
            serving.Id.Value,
            serving.Quantity,
            serving.Unit,
            serving.Label,
            serving.EquivalentQuantity,
            nutrition.Kilocalories,
            nutrition.ProteinGrams,
            nutrition.CarbohydratesGrams,
            nutrition.FatGrams
        );
    }
}
