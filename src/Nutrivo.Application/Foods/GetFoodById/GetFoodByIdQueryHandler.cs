using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.GetFoodById;

public sealed class GetFoodByIdQueryHandler(IFoodQueries foodQueries)
    : IQueryHandler<GetFoodByIdQuery, FoodReadModel>
{
    public async Task<Result<FoodReadModel>> HandleAsync(
        GetFoodByIdQuery query,
        CancellationToken cancellationToken
    )
    {
        var id = FoodId.Create(query.Id);
        if (id.IsFailure)
        {
            return Result<FoodReadModel>.Failure(id.Error!);
        }

        var food = await foodQueries.GetByIdAsync(id.Value, cancellationToken);
        return food is null
            ? Result<FoodReadModel>.Failure(GetFoodByIdErrors.NotFound)
            : Result<FoodReadModel>.Success(food);
    }
}
