using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Targets;

namespace Nutrivo.Application.Targets.SetDailyNutritionTarget;

public sealed class SetDailyNutritionTargetCommandHandler(
    IDailyNutritionTargetRepository repository
) : ICommandHandler<SetDailyNutritionTargetCommand, DailyNutritionTargetResult>
{
    public async Task<Result<DailyNutritionTargetResult>> HandleAsync(
        SetDailyNutritionTargetCommand command,
        CancellationToken cancellationToken
    )
    {
        var candidate = DailyNutritionTarget.Create(
            command.Kilocalories,
            command.ProteinGrams,
            command.CarbohydratesGrams,
            command.FatGrams
        );
        if (candidate.IsFailure)
        {
            return Result<DailyNutritionTargetResult>.Failure(candidate.Error!);
        }

        await repository.SetCurrentAsync(candidate.Value, cancellationToken);
        return Result<DailyNutritionTargetResult>.Success(
            new DailyNutritionTargetResult(
                DailyNutritionTargetValues.From(candidate.Value)
            )
        );
    }
}
