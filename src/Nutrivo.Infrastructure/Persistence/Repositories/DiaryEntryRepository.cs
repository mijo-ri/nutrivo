using Nutrivo.Application.Diaries;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Infrastructure.Persistence.Repositories;

internal sealed class DiaryEntryRepository(NutrivoDbContext context)
    : IDiaryEntryRepository
{
    public async Task AddAsync(DiaryEntry entry, CancellationToken cancellationToken)
    {
        context.DiaryEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
