using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Foods.SearchFoods;

public sealed class SearchFoodsQueryHandler(IFoodQueries foodQueries)
    : IQueryHandler<SearchFoodsQuery, SearchFoodsResult>
{
    public async Task<Result<SearchFoodsResult>> HandleAsync(
        SearchFoodsQuery query,
        CancellationToken cancellationToken
    )
    {
        if (query.Page < 1)
        {
            return Result<SearchFoodsResult>.Failure(SearchFoodsErrors.InvalidPage);
        }

        if (query.PageSize is < 1 or > 100)
        {
            return Result<SearchFoodsResult>.Failure(SearchFoodsErrors.InvalidPageSize);
        }

        if ((query.Page - 1L) * query.PageSize > int.MaxValue)
        {
            return Result<SearchFoodsResult>.Failure(SearchFoodsErrors.InvalidPage);
        }

        var search = query.Search?.Trim();
        var result = await foodQueries.SearchAsync(
            string.IsNullOrEmpty(search) ? null : search,
            query.Page,
            query.PageSize,
            cancellationToken
        );

        return Result<SearchFoodsResult>.Success(result);
    }
}
