using Nutrivo.Domain.Targets;

namespace Nutrivo.Application.Targets;

public interface IDailyNutritionTargetRepository
{
    Task<DailyNutritionTarget?> GetCurrentAsync(CancellationToken cancellationToken);

    Task SetCurrentAsync(
        DailyNutritionTarget target,
        CancellationToken cancellationToken
    );
}
