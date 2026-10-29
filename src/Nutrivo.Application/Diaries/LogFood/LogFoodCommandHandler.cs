using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Diaries.LogFood;

public sealed class LogFoodCommandHandler(
    IFoodRepository foodRepository,
    IDiaryEntryRepository diaryEntryRepository
) : ICommandHandler<LogFoodCommand, DiaryEntryId>
{
    public async Task<Result<DiaryEntryId>> HandleAsync(
        LogFoodCommand command,
        CancellationToken cancellationToken
    )
    {
        var direct = command.Quantity.HasValue
            && command.ReferenceUnit.HasValue
            && !command.ServingSizeId.HasValue
            && !command.ServingCount.HasValue;
        var serving = command.ServingSizeId.HasValue
            && command.ServingCount.HasValue
            && !command.Quantity.HasValue
            && !command.ReferenceUnit.HasValue;

        if (!direct && !serving)
        {
            return Result<DiaryEntryId>.Failure(LogFoodErrors.InvalidAmount);
        }

        var foodId = FoodId.Create(command.FoodId);
        if (foodId.IsFailure)
        {
            return Result<DiaryEntryId>.Failure(foodId.Error!);
        }

        ServingSizeId? servingSizeId = null;
        if (serving)
        {
            var id = ServingSizeId.Create(command.ServingSizeId!.Value);
            if (id.IsFailure)
            {
                return Result<DiaryEntryId>.Failure(id.Error!);
            }

            servingSizeId = id.Value;
        }

        var food = await foodRepository.GetByIdAsync(foodId.Value, cancellationToken);
        if (food is null)
        {
            return Result<DiaryEntryId>.Failure(LogFoodErrors.FoodNotFound);
        }

        var entry = direct
            ? DiaryEntry.Create(
                food,
                command.Date,
                command.MealType,
                command.Quantity!.Value,
                command.ReferenceUnit!.Value
            )
            : DiaryEntry.CreateFromServing(
                food,
                command.Date,
                command.MealType,
                servingSizeId,
                command.ServingCount!.Value
            );

        if (entry.IsFailure)
        {
            return Result<DiaryEntryId>.Failure(entry.Error!);
        }

        await diaryEntryRepository.AddAsync(entry.Value, cancellationToken);
        return Result<DiaryEntryId>.Success(entry.Value.Id);
    }
}
