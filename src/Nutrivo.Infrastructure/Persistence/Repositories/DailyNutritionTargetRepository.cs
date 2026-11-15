using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Targets;
using Nutrivo.Domain.Targets;

namespace Nutrivo.Infrastructure.Persistence.Repositories;

public sealed class DailyNutritionTargetRepository(NutrivoDbContext context)
    : IDailyNutritionTargetRepository
{
    public Task<DailyNutritionTarget?> GetCurrentAsync(
        CancellationToken cancellationToken
    )
    {
        return context
            .Set<DailyNutritionTarget>()
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SetCurrentAsync(
        DailyNutritionTarget target,
        CancellationToken cancellationToken
    )
    {
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO public.daily_nutrition_targets
                (id, kilocalories, protein_grams, carbohydrates_grams, fat_grams)
            VALUES (1, {target.Kilocalories}, {target.ProteinGrams}, {target.CarbohydratesGrams}, {target.FatGrams})
            ON CONFLICT (id) DO UPDATE SET
                kilocalories = EXCLUDED.kilocalories,
                protein_grams = EXCLUDED.protein_grams,
                carbohydrates_grams = EXCLUDED.carbohydrates_grams,
                fat_grams = EXCLUDED.fat_grams
            """,
            cancellationToken
        );
    }
}
