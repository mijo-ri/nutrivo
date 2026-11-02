using Nutrivo.Application.Diaries.GetDiaryDay;

namespace Nutrivo.Application.Diaries;

public interface IDiaryQueries
{
    Task<GetDiaryDayResult> GetDayAsync(
        DateOnly date,
        CancellationToken cancellationToken
    );
}
