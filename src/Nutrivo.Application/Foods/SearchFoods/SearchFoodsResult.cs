using System.Collections.Immutable;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.SearchFoods;

public sealed record SearchFoodsResult(
    ImmutableArray<FoodSearchItem> Items,
    int Page,
    int PageSize,
    int TotalCount
);

public sealed record FoodSearchItem(
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
