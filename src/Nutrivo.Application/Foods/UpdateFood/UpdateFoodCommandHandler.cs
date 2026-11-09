using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.UpdateFood;

public sealed class UpdateFoodCommandHandler(IFoodRepository foodRepository)
    : ICommandHandler<UpdateFoodCommand, FoodId>
{
    public async Task<Result<FoodId>> HandleAsync(
        UpdateFoodCommand command,
        CancellationToken cancellationToken
    )
    {
        var id = FoodId.Create(command.Id);
        if (id.IsFailure)
        {
            return Result<FoodId>.Failure(id.Error!);
        }

        var food = await foodRepository.GetForUpdateAsync(id.Value, cancellationToken);
        if (food is null)
        {
            return Result<FoodId>.Failure(UpdateFoodErrors.NotFound);
        }

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

        var servings = new List<ServingSizeUpdate?>();
        foreach (var input in command.ServingSizes ?? [])
        {
            if (input is null)
            {
                return Result<FoodId>.Failure(FoodErrors.ServingSizeRequired);
            }

            ServingSizeId? servingId = null;
            if (input.Id.HasValue)
            {
                var parsedId = ServingSizeId.Create(input.Id.Value);
                if (parsedId.IsFailure)
                {
                    return Result<FoodId>.Failure(parsedId.Error!);
                }

                servingId = parsedId.Value;
            }

            servings.Add(
                new ServingSizeUpdate(
                    servingId,
                    input.Quantity,
                    input.Unit,
                    input.Label,
                    input.EquivalentQuantity
                )
            );
        }

        var updated = food.Update(
            command.Name,
            command.Brand,
            command.ReferenceQuantity,
            command.ReferenceUnit,
            nutrition.Value,
            servings
        );
        if (updated.IsFailure)
        {
            return Result<FoodId>.Failure(updated.Error!);
        }

        await foodRepository.SaveAsync(food, cancellationToken);
        return Result<FoodId>.Success(food.Id);
    }
}
