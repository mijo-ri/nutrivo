using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Foods.SearchFoods;

public static class SearchFoodsErrors
{
    public static readonly DomainError InvalidPage = new(
        "SearchFoods.Page.Invalid",
        "Page must be at least 1 and its offset must not exceed 2147483647."
    );

    public static readonly DomainError InvalidPageSize = new(
        "SearchFoods.PageSize.Invalid",
        "Page size must be between 1 and 100."
    );
}
