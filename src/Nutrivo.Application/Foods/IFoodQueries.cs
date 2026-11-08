using Nutrivo.Application.Foods.SearchFoods;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods;

public interface IFoodQueries
{
    Task<FoodReadModel?> GetByIdAsync(FoodId id, CancellationToken cancellationToken);

    Task<SearchFoodsResult> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
