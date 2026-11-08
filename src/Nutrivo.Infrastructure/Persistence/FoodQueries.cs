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
            .Select(Projection)
            .ToListAsync(cancellationToken);

        var items = rows.Select(MapFood).ToImmutableArray();

        return new SearchFoodsResult(
            items,
            page,
            pageSize,
            totalCount
        );
    }

    public async Task<FoodReadModel?> GetByIdAsync(
        FoodId id,
        CancellationToken cancellationToken
    )
    {
        var row = await context
            .Foods.AsNoTracking()
            .Where(food => food.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : MapFood(row);
    }

    private sealed record FoodRow(
        FoodId Id,
        string Name,
        string? Brand,
        decimal ReferenceQuantity,
        ReferenceUnit ReferenceUnit,
        NutritionalValues NutritionalValues,
        List<ServingSize> ServingSizes
    );

    private static readonly System.Linq.Expressions.Expression<Func<Food, FoodRow>>
        Projection =
            food => new FoodRow(
                food.Id,
                food.Name,
                food.Brand,
                food.ReferenceQuantity,
                food.ReferenceUnit,
                food.NutritionalValues,
                food.ServingSizes.OrderBy(serving => serving.Position).ToList()
            );

    private static FoodReadModel MapFood(FoodRow food)
    {
        return new FoodReadModel(
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
        );
    }

    private static ServingSizeReadModel MapServing(
        ServingSize serving,
        NutritionalValues basis,
        decimal referenceQuantity
    )
    {
        var nutrition = basis.Scale(serving.EquivalentQuantity, referenceQuantity).Value;
        return new ServingSizeReadModel(
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
