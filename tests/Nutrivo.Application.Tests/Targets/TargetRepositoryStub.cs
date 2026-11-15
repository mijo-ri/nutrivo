using Nutrivo.Application.Targets;
using Nutrivo.Domain.Targets;

namespace Nutrivo.Application.Tests.Targets;

internal sealed class TargetRepositoryStub : IDailyNutritionTargetRepository
{
    public DailyNutritionTarget? Current { get; private set; }
    public int Saves { get; private set; }
    public CancellationToken LastToken { get; private set; }

    public Task<DailyNutritionTarget?> GetCurrentAsync(
        CancellationToken cancellationToken
    )
    {
        LastToken = cancellationToken;
        return Task.FromResult(Current);
    }

    public Task SetCurrentAsync(
        DailyNutritionTarget target,
        CancellationToken cancellationToken
    )
    {
        LastToken = cancellationToken;
        Current = target;
        Saves++;
        return Task.CompletedTask;
    }
}
