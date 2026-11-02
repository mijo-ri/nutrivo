using System.Collections.Immutable;
using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Application.Diaries.GetDiaryDay;

public sealed class GetDiaryDayQueryHandler(IDiaryQueries diaryQueries)
    : IQueryHandler<GetDiaryDayQuery, GetDiaryDayResult>
{
    public async Task<Result<GetDiaryDayResult>> HandleAsync(
        GetDiaryDayQuery query,
        CancellationToken cancellationToken
    )
    {
        var entries = await diaryQueries.GetDayAsync(query.Date, cancellationToken);

        try
        {
            var totals = Sum(entries);
            MealType[] mealTypes =
            [
                MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snacks
            ];
            var mealTotals = mealTypes
                .Select(meal => new DiaryMealTotals(
                        meal,
                        Sum(entries.Where(entry => entry.MealType == meal))
                    )
                )
                .ToImmutableArray();

            return Result<GetDiaryDayResult>.Success(
                new GetDiaryDayResult(query.Date, entries, totals, mealTotals)
            );
        }
        catch (OverflowException)
        {
            return Result<GetDiaryDayResult>.Failure(GetDiaryDayErrors.TotalsOutOfRange);
        }
    }

    private static DiaryNutritionTotals Sum(IEnumerable<DiaryDayEntry> entries)
    {
        decimal kilocalories = 0;
        decimal proteinGrams = 0;
        decimal carbohydratesGrams = 0;
        decimal fatGrams = 0;

        foreach (var entry in entries)
        {
            kilocalories += entry.Kilocalories;
            proteinGrams += entry.ProteinGrams;
            carbohydratesGrams += entry.CarbohydratesGrams;
            fatGrams += entry.FatGrams;
        }

        return new DiaryNutritionTotals(
            kilocalories,
            proteinGrams,
            carbohydratesGrams,
            fatGrams
        );
    }
}
