namespace Nutrivo.Application.Foods.SearchFoods;

public sealed record SearchFoodsQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20
);
