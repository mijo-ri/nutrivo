using Nutrivo.Application.Foods.SearchFoods;

namespace Nutrivo.Application.Foods;

public interface IFoodQueries
{
    Task<SearchFoodsResult> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
