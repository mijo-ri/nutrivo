using Microsoft.EntityFrameworkCore;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Foods;

internal static class DeleteFoodTestData
{
    internal static Food Food(string name, bool servings = true) => Nutrivo.Domain.Foods
        .Food.Create(
            name,
            "Brand",
            100m,
            ReferenceUnit.Gram,
            NutritionalValues.Create(123.123456789m, 1.005m, 2.345m, 3.456m).Value,
            servings
                ?
                [
                    ServingSize.Create(1m, "cup", null, 50m).Value,
                    ServingSize.Create(2m, "pieces", "Large", 75m).Value
                ]
                : []
        )
        .Value;

    internal static async Task<(DbContextOptions<NutrivoDbContext> Options, Food Food)>
        SeedAsync(
            PostgreSqlFixture fixture,
            bool servings = true
        )
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        var food = Food("Target", servings);
        var other = Food("Unrelated");
        context.Foods.AddRange(food, other);
        if (servings)
        {
            foreach (var day in new[]
                     {
                         new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 11)
                     })
            {
                context.DiaryEntries.AddRange(
                    DiaryEntry.Create(
                            food,
                            day,
                            MealType.Lunch,
                            33.333m,
                            ReferenceUnit.Gram
                        )
                        .Value,
                    DiaryEntry.CreateFromServing(
                            food,
                            day,
                            MealType.Dinner,
                            food.ServingSizes[0].Id,
                            0.5m
                        )
                        .Value,
                    DiaryEntry.Create(
                            other,
                            day,
                            MealType.Snacks,
                            20m,
                            ReferenceUnit.Gram
                        )
                        .Value
                );
            }
        }

        await context.SaveChangesAsync();
        return (options, food);
    }

    internal static async Task RejectServingDeletionAsync(
        DbContextOptions<NutrivoDbContext> options
    )
    {
        await using var context = new NutrivoDbContext(options);
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE FUNCTION reject_serving_delete() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'secret-storage-details'; END; $$;
            CREATE TRIGGER reject_serving_delete BEFORE DELETE ON public.food_serving_sizes
            FOR EACH ROW EXECUTE FUNCTION reject_serving_delete();
            """
        );
    }

    internal static async Task<string> StateAsync(
        DbContextOptions<NutrivoDbContext> options
    )
    {
        await using var context = new NutrivoDbContext(options);
        // Read actual stored rows, including exact numeric values and capture metadata.
        var rows = await context
            .Database.SqlQueryRaw<string>(
                """
                SELECT jsonb_build_object(
                    'foods', (SELECT jsonb_agg(to_jsonb(f) ORDER BY id) FROM public.foods f),
                    'servings', (SELECT jsonb_agg(to_jsonb(s) ORDER BY id) FROM public.food_serving_sizes s),
                    'diary', (SELECT jsonb_agg(to_jsonb(d) ORDER BY id) FROM public.diary_entries d)
                )::text AS "Value"
                """
            )
            .SingleAsync();
        return rows;
    }
}
