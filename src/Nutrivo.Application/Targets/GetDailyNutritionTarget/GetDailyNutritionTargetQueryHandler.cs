using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Targets.GetDailyNutritionTarget;

public sealed class GetDailyNutritionTargetQueryHandler(
    IDailyNutritionTargetRepository repository
) : IQueryHandler<GetDailyNutritionTargetQuery, DailyNutritionTargetResult>
{
    public async Task<Result<DailyNutritionTargetResult>> HandleAsync(
        GetDailyNutritionTargetQuery query,
        CancellationToken cancellationToken
    )
    {
        var target = await repository.GetCurrentAsync(cancellationToken);
        return Result<DailyNutritionTargetResult>.Success(
            new(target is null ? null : DailyNutritionTargetValues.From(target))
        );
    }
}
