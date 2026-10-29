using Microsoft.EntityFrameworkCore;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence;

public sealed class NutrivoDbContext(DbContextOptions<NutrivoDbContext> options)
    : DbContext(options)
{
    public DbSet<Food> Foods
    {
        get
        {
            return Set<Food>();
        }
    }

    public DbSet<DiaryEntry> DiaryEntries
    {
        get
        {
            return Set<DiaryEntry>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NutrivoDbContext).Assembly);
    }
}
