using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.DeleteFood;

public sealed class DeleteFoodCommandHandler(IFoodRepository foodRepository)
    : ICommandHandler<DeleteFoodCommand, FoodId>
{
    public async Task<Result<FoodId>> HandleAsync(
        DeleteFoodCommand command,
        CancellationToken cancellationToken
    )
    {
        var id = FoodId.Create(command.Id);
        if (id.IsFailure)
        {
            return Result<FoodId>.Failure(id.Error!);
        }

        return await foodRepository.DeleteAsync(id.Value, cancellationToken)
            ? Result<FoodId>.Success(id.Value)
            : Result<FoodId>.Failure(DeleteFoodErrors.NotFound);
    }
}
