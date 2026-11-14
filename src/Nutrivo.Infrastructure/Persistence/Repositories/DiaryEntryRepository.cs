using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Diaries;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Infrastructure.Persistence.Repositories;

internal sealed class DiaryEntryRepository(NutrivoDbContext context)
    : IDiaryEntryRepository
{
    public Task<DiaryEntry?> GetForUpdateAsync(
        DiaryEntryId id,
        CancellationToken cancellationToken
    ) =>
        context.DiaryEntries.SingleOrDefaultAsync(
            entry => entry.Id == id,
            cancellationToken
        );

    public async Task<bool> SaveAsync(
        DiaryEntry entry,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Even an unchanged amount must check that the target still exists.
            context.Entry(entry).Property(value => value.Quantity).IsModified = true;
            context.Entry(entry).Property(value => value.ServingCount).IsModified = true;
            context
                .Entry(entry)
                .ComplexProperty(value => value.NutritionalValues)
                .IsModified = true;
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(
        DiaryEntryId id,
        CancellationToken cancellationToken
    ) =>
        await context
            .DiaryEntries.Where(entry => entry.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
        == 1;

    public async Task AddAsync(DiaryEntry entry, CancellationToken cancellationToken)
    {
        context.DiaryEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
