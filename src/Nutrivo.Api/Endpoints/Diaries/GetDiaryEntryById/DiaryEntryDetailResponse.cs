using Nutrivo.Application.Diaries.GetDiaryEntryById;
using Nutrivo.Api.Endpoints.Diaries.GetDiaryDay;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Diaries.GetDiaryEntryById;

public sealed record DiaryFoodSnapshotResponse(
    Guid Id,
    string Name,
    string? Brand,
    decimal ReferenceQuantity,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams
);

public sealed record DiaryEntryDetailResponse(
    Guid Id,
    DateOnly Date,
    MealType MealType,
    decimal Quantity,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams,
    decimal? ServingCount,
    DiaryDayServingSizeResponse? ServingSize,
    DiaryFoodSnapshotResponse Food
)
{
    internal static DiaryEntryDetailResponse From(DiaryEntryDetail entry) => new(
        entry.Id,
        entry.Date,
        entry.MealType,
        entry.Quantity,
        entry.ReferenceUnit,
        NutritionRounding.ForDisplay(entry.Kilocalories),
        NutritionRounding.ForDisplay(entry.ProteinGrams),
        NutritionRounding.ForDisplay(entry.CarbohydratesGrams),
        NutritionRounding.ForDisplay(entry.FatGrams),
        entry.ServingCount,
        entry.ServingSize is { } serving
            ? new DiaryDayServingSizeResponse(
                serving.Id,
                serving.Quantity,
                serving.Unit,
                serving.Label,
                serving.EquivalentQuantity
            )
            : null,
        new DiaryFoodSnapshotResponse(
            entry.Food.Id,
            entry.Food.Name,
            entry.Food.Brand,
            entry.Food.ReferenceQuantity,
            entry.Food.ReferenceUnit,
            entry.Food.Kilocalories,
            entry.Food.ProteinGrams,
            entry.Food.CarbohydratesGrams,
            entry.Food.FatGrams
        )
    );
}
