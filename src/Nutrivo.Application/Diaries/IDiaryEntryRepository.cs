using Nutrivo.Domain.Diaries;

namespace Nutrivo.Application.Diaries;

public interface IDiaryEntryRepository
{
    /// <summary>
    /// Adds a diary entry and commits it to persistent storage before returning.
    /// </summary>
    Task AddAsync(DiaryEntry entry, CancellationToken cancellationToken);
}
