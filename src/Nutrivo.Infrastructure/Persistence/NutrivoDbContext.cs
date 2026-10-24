using Microsoft.EntityFrameworkCore;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Infrastructure.Persistence;

public sealed class NutrivoDbContext(DbContextOptions<NutrivoDbContext> options)
    : DbContext(options)
{
    public DbSet<Food> Foods => Set<Food>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NutrivoDbContext).Assembly);
    }
}
