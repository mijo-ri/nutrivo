using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.GetDiaryEntryById;
using Nutrivo.Domain.Diaries;
using Nutrivo.Application.Diaries.GetDiaryDay;

namespace Nutrivo.Infrastructure.Persistence;

internal sealed class DiaryQueries(NutrivoDbContext context) : IDiaryQueries
{
    public Task<DiaryEntryDetail?> GetByIdAsync(
        DiaryEntryId id,
        CancellationToken cancellationToken
    ) =>
        context
            .DiaryEntries.AsNoTracking()
            .Where(entry => entry.Id == id)
            .Select(entry => new DiaryEntryDetail(
                    entry.Id.Value,
                    entry.Date,
                    entry.MealType,
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
                        ),
                    new DiaryFoodSnapshot(
                        entry.FoodSnapshot.FoodId.Value,
                        entry.FoodSnapshot.Name,
                        entry.FoodSnapshot.Brand,
                        entry.FoodSnapshot.ReferenceQuantity,
                        entry.FoodSnapshot.ReferenceUnit,
                        entry.FoodSnapshot.NutritionalValues.Kilocalories,
                        entry.FoodSnapshot.NutritionalValues.ProteinGrams,
                        entry.FoodSnapshot.NutritionalValues.CarbohydratesGrams,
                        entry.FoodSnapshot.NutritionalValues.FatGrams
                    )
                )
            )
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ImmutableArray<DiaryDayEntry>> GetDayAsync(
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

        return entries.ToImmutableArray();
    }
}
