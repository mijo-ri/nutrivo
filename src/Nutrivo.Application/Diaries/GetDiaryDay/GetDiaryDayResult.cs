using System.Collections.Immutable;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Diaries.GetDiaryDay;

public sealed record GetDiaryDayResult(
    DateOnly Date,
    ImmutableArray<DiaryDayEntry> Entries
);

public sealed record DiaryDayEntry(
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
    DiaryDayServingSize? ServingSize
);

public sealed record DiaryDayServingSize(
    Guid Id,
    decimal Quantity,
    string Unit,
    string? Label,
    decimal EquivalentQuantity
);
