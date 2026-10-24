using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.CreateFood;

public sealed class CreateFoodCommandHandler(IFoodRepository foodRepository)
{
    public async Task<Result<FoodId>> HandleAsync(
        CreateFoodCommand command,
        CancellationToken cancellationToken
    )
    {
        var nutrition = NutritionalValues.Create(
            command.Kilocalories,
            command.ProteinGrams,
            command.CarbohydratesGrams,
            command.FatGrams
        );

        if (nutrition.IsFailure)
        {
            return Result<FoodId>.Failure(nutrition.Error!);
        }

        var food = Food.Create(
            command.Name,
            command.Brand,
            command.ReferenceUnit,
            nutrition.Value
        );

        if (food.IsFailure)
        {
            return Result<FoodId>.Failure(food.Error!);
        }

        await foodRepository.AddAsync(food.Value, cancellationToken);

        return Result<FoodId>.Success(food.Value.Id);
    }
}
