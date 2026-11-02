using System.Collections.Immutable;
using Nutrivo.Application.Diaries.GetDiaryDay;

namespace Nutrivo.Application.Diaries;

public interface IDiaryQueries
{
    Task<ImmutableArray<DiaryDayEntry>> GetDayAsync(
        DateOnly date,
        CancellationToken cancellationToken
    );
}
