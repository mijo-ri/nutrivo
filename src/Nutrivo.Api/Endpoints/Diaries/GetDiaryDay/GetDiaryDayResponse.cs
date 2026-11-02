using System.Collections.Immutable;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Diaries.GetDiaryDay;

public sealed record GetDiaryDayResponse(
    DateOnly Date,
    ImmutableArray<DiaryDayEntryResponse> Entries,
    DiaryNutritionTotalsResponse Totals,
    ImmutableArray<DiaryMealTotalsResponse> MealTotals
);

public sealed record DiaryNutritionTotalsResponse(
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams
);

public sealed record DiaryMealTotalsResponse(
    MealType MealType,
    DiaryNutritionTotalsResponse Totals
);

public sealed record DiaryDayEntryResponse(
    Guid Id,
    MealType MealType,
    Guid FoodId,
    string Name,
    string? Brand,
    decimal Quantity,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams,
    decimal? ServingCount,
    DiaryDayServingSizeResponse? ServingSize
);

public sealed record DiaryDayServingSizeResponse(
    Guid Id,
    decimal Quantity,
    string Unit,
    string? Label,
    decimal EquivalentQuantity
);
