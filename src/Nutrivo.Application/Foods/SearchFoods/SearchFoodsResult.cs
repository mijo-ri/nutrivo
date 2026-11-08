using System.Collections.Immutable;

namespace Nutrivo.Application.Foods.SearchFoods;

public sealed record SearchFoodsResult(
    ImmutableArray<FoodReadModel> Items,
    int Page,
    int PageSize,
    int TotalCount
);
