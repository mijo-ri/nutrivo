using System.Collections.Immutable;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods;

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
    decimal FatGrams
);
