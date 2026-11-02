using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.GetDiaryDay;

namespace Nutrivo.Infrastructure.Persistence;

internal sealed class DiaryQueries(NutrivoDbContext context) : IDiaryQueries
{
    public async Task<GetDiaryDayResult> GetDayAsync(
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        var entries = await context
            .DiaryEntries
            .AsNoTracking()
            .Where(entry => entry.Date == date)
            .OrderBy(entry => entry.MealType)
            .ThenBy(entry => EF.Property<DateTimeOffset>(entry, "CreatedAt"))
            .ThenBy(entry => entry.Id)
            .Select(entry => new DiaryDayEntry(
                    entry.Id.Value,
                    entry.MealType,
                    entry.FoodSnapshot.FoodId.Value,
                    entry.FoodSnapshot.Name,
                    entry.FoodSnapshot.Brand,
                    entry.Quantity,
                    entry.ReferenceUnit,
                    entry.NutritionalValues.Kilocalories,
                    entry.NutritionalValues.ProteinGrams,
                    entry.NutritionalValues.CarbohydratesGrams,
                    entry.NutritionalValues.FatGrams,
                    entry.ServingCount,
                    entry.ServingSizeSnapshot == null
                        ? null
                        : new DiaryDayServingSize(
                            entry.ServingSizeSnapshot.ServingSizeId.Value,
                            entry.ServingSizeSnapshot.Quantity,
                            entry.ServingSizeSnapshot.Unit,
                            entry.ServingSizeSnapshot.Label,
                            entry.ServingSizeSnapshot.EquivalentQuantity
                        )
                )
            )
            .ToListAsync(cancellationToken);

        return new GetDiaryDayResult(date, entries.ToImmutableArray());
    }
}
