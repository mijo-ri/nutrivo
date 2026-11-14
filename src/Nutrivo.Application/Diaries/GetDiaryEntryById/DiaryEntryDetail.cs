using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;
using Nutrivo.Application.Diaries.GetDiaryDay;

namespace Nutrivo.Application.Diaries.GetDiaryEntryById;

public sealed record DiaryEntryDetail(
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
    DiaryDayServingSize? ServingSize,
    DiaryFoodSnapshot Food
);

public sealed record DiaryFoodSnapshot(
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
