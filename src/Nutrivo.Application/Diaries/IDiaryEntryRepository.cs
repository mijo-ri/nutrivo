using Nutrivo.Domain.Diaries;

namespace Nutrivo.Application.Diaries;

public interface IDiaryEntryRepository
{
    Task<DiaryEntry?> GetForUpdateAsync(
        DiaryEntryId id,
        CancellationToken cancellationToken
    );

    // False means a concurrent deletion removed the row before commit.
    Task<bool> SaveAsync(DiaryEntry entry, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(DiaryEntryId id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a diary entry and commits it to persistent storage before returning.
    /// </summary>
    Task AddAsync(DiaryEntry entry, CancellationToken cancellationToken);
}
