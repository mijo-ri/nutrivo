using System.Collections.Immutable;
using Nutrivo.Application.Diaries.GetDiaryDay;

namespace Nutrivo.Application.Diaries;

public interface IDiaryQueries
{
    Task<Nutrivo.Application.Diaries.GetDiaryEntryById.DiaryEntryDetail?> GetByIdAsync(
        Nutrivo.Domain.Diaries.DiaryEntryId id,
        CancellationToken cancellationToken
    );

    Task<ImmutableArray<DiaryDayEntry>> GetDayAsync(
        DateOnly date,
        CancellationToken cancellationToken
    );
}
