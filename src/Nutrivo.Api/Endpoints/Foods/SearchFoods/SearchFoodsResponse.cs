using System.Collections.Immutable;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods.SearchFoods;

public sealed record SearchFoodsResponse(
    ImmutableArray<SearchFoodResponse> Items,
    int Page,
    int PageSize,
    int TotalCount
);

public sealed record SearchFoodResponse(
    Guid Id,
    string Name,
    string? Brand,
    decimal ReferenceQuantity,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams,
    ImmutableArray<SearchServingSizeResponse> ServingSizes
);

public sealed record SearchServingSizeResponse(
    Guid Id,
    decimal Quantity,
    string Unit,
    string? Label,
    decimal EquivalentQuantity,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams
);
