using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.CreateFood;

public sealed class CreateFoodCommandHandler(IFoodRepository foodRepository)
    : ICommandHandler<CreateFoodCommand, FoodId>
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

        var servings = new List<ServingSize>();
        foreach (var input in command.ServingSizes ?? [])
        {
            if (input is null)
            {
                return Result<FoodId>.Failure(FoodErrors.ServingSizeRequired);
            }

            var serving = ServingSize.Create(
                input.Quantity,
                input.Unit,
                input.Label,
                input.EquivalentQuantity
            );
            if (serving.IsFailure)
            {
                return Result<FoodId>.Failure(serving.Error!);
            }

            servings.Add(serving.Value);
        }

        var food = Food.Create(
            command.Name,
            command.Brand,
            command.ReferenceQuantity,
            command.ReferenceUnit,
            nutrition.Value,
            servings
        );

        if (food.IsFailure)
        {
            return Result<FoodId>.Failure(food.Error!);
        }

        await foodRepository.AddAsync(food.Value, cancellationToken);

        return Result<FoodId>.Success(food.Value.Id);
    }
}
