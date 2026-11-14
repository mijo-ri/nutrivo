using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Api.Endpoints.Diaries.GetDiaryDay;
using Nutrivo.Api.Endpoints.Diaries.LogFood;
using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.GetDiaryDay;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Foods;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Diaries;

[Collection("PostgreSQL")]
public sealed class GetDiaryDayEndpointTests(PostgreSqlFixture fixture)
{
    private static readonly DateOnly Day = new(2026, 11, 2);
    private static readonly DiaryNutritionTotalsResponse ZeroTotals = new(0m, 0m, 0m, 0m);

    [Theory]
    [InlineData(ReferenceUnit.Gram, false, null)]
    [InlineData(ReferenceUnit.Milliliter, false, "Brand")]
    [InlineData(ReferenceUnit.Gram, true, "Brand")]
    [InlineData(ReferenceUnit.Milliliter, true, null)]
    public async Task Get_ReturnsPostedEntryWithSnapshotsAndRoundsOnlyNutrition(
        ReferenceUnit unit,
        bool serving,
        string? brand
    )
    {
        var food = CreateFood("Egg", unit, brand);
        var options = await CreateDatabaseAsync(food);
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        using var posted = await client.PostAsJsonAsync(
            "/diary-entries",
            new
            {
                foodId = food.Id.Value,
                date = Day,
                mealType = MealType.Lunch,
                quantity = serving ? (decimal?)null : 100.123m,
                referenceUnit = serving ? (ReferenceUnit?)null : unit,
                servingSizeId =
                    serving ? food.ServingSizes.Single().Id.Value : (Guid?)null,
                servingCount = serving ? 0.5m : (decimal?)null
            }
        );
        Assert.Equal(HttpStatusCode.Created, posted.StatusCode);
        var id = (await posted.Content.ReadFromJsonAsync<LogFoodResponse>())!.Id;

        var result = await GetAsync(client, Day);

        Assert.Equal(Day, result.Date);
        var entry = Assert.Single(result.Entries);
        Assert.Equal(id, entry.Id);
        Assert.Equal(MealType.Lunch, entry.MealType);
        Assert.Equal(food.Id.Value, entry.FoodId);
        Assert.Equal("Egg", entry.Name);
        Assert.Equal(brand, entry.Brand);
        Assert.Equal(100.123m, entry.Quantity);
        Assert.Equal(unit, entry.ReferenceUnit);
        Assert.Equal(150.01m, entry.Kilocalories);
        Assert.Equal(10.01m, entry.ProteinGrams);
        Assert.Equal(1.01m, entry.CarbohydratesGrams);
        Assert.Equal(5.01m, entry.FatGrams);
        Assert.Equal(
            new DiaryNutritionTotalsResponse(150.01m, 10.01m, 1.01m, 5.01m),
            result.Totals
        );
        AssertMealOrder(result);
        Assert.Equal(result.Totals, result.MealTotals[1].Totals);
        Assert.All(
            result.MealTotals.Where(meal => meal.MealType != MealType.Lunch),
            meal => Assert.Equal(ZeroTotals, meal.Totals)
        );
        if (serving)
        {
            Assert.Equal(0.5m, entry.ServingCount);
            var portion = Assert.IsType<DiaryDayServingSizeResponse>(entry.ServingSize);
            Assert.Equal(food.ServingSizes.Single().Id.Value, portion.Id);
            Assert.Equal(2m, portion.Quantity);
            Assert.Equal("pieces", portion.Unit);
            Assert.Equal("Medium", portion.Label);
            Assert.Equal(200.246m, portion.EquivalentQuantity);
        }
        else
        {
            Assert.Null(entry.ServingCount);
            Assert.Null(entry.ServingSize);
        }

        await using var verification = new NutrivoDbContext(options);
        var stored = await verification.DiaryEntries.SingleAsync();
        Assert.Equal(150.005m, stored.NutritionalValues.Kilocalories);
        Assert.Equal(10.005m, stored.NutritionalValues.ProteinGrams);
        Assert.Equal(1.005m, stored.NutritionalValues.CarbohydratesGrams);
        Assert.Equal(5.005m, stored.NutritionalValues.FatGrams);
        using var response =
            await client.GetAsync($"/diary-entries?date={Day:yyyy-MM-dd}");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            150.01m,
            json.GetProperty("totals").GetProperty("kilocalories").GetDecimal()
        );
        Assert.Equal(4, json.GetProperty("mealTotals").GetArrayLength());
        var item = Assert.Single(json.GetProperty("entries").EnumerateArray());
        Assert.False(item.TryGetProperty("createdAt", out _));
        if (!serving)
        {
            Assert.Equal(JsonValueKind.Null, item.GetProperty("servingCount").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("servingSize").ValueKind);
        }
    }

    [Theory]
    [InlineData(2020, 2, 29)]
    [InlineData(2030, 1, 1)]
    public async Task Get_FiltersCalendarDateAndPreservesRepeatedEntries(
        int year,
        int month,
        int day
    )
    {
        var date = new DateOnly(year, month, day);
        var food = CreateFood("Oats");
        var options = await CreateDatabaseAsync(food);
        var first = CreateEntry(food, date);
        var second = CreateEntry(food, date);
        await using (var context = new NutrivoDbContext(options))
        {
            context.DiaryEntries.AddRange(
                CreateEntry(food, date.AddDays(-1)),
                first,
                second,
                CreateEntry(food, date.AddDays(1))
            );
            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var result = await GetAsync(client, date);

        Assert.Equal(date, result.Date);
        Assert.Equal(
            new[]
            {
                first.Id.Value, second.Id.Value
            }.Order(),
            result.Entries.Select(entry => entry.Id).Order()
        );
        Assert.Equal(
            new DiaryNutritionTotalsResponse(300.01m, 20.01m, 2.01m, 10.01m),
            result.Totals
        );
        Assert.Equal(result.Totals, result.MealTotals[0].Totals);
        var empty = await GetAsync(client, date.AddDays(2));
        Assert.Empty(empty.Entries);
        Assert.Equal(ZeroTotals, empty.Totals);
        Assert.All(empty.MealTotals, meal => Assert.Equal(ZeroTotals, meal.Totals));
    }

    [Fact]
    public async Task Get_WithEmptyDatabase_ReturnsRequestedDateAndEmptyEntries()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var result = await GetAsync(client, Day);

        Assert.Equal(Day, result.Date);
        Assert.Empty(result.Entries);
        Assert.Equal(ZeroTotals, result.Totals);
        AssertMealOrder(result);
        Assert.All(result.MealTotals, meal => Assert.Equal(ZeroTotals, meal.Totals));
    }

    [Fact]
    public async Task Get_OrdersByMealThenCaptureTimeThenId()
    {
        var apple = CreateFood("Apple");
        var zebra = CreateFood("Zebra");
        var options = await CreateDatabaseAsync(apple, zebra);
        var breakfastEarly = CreateEntry(zebra, Day);
        var breakfastLate = CreateEntry(apple, Day);
        var lunchOne = CreateEntry(zebra, Day, MealType.Lunch);
        var lunchTwo = CreateEntry(apple, Day, MealType.Lunch);
        var dinner = CreateEntry(apple, Day, MealType.Dinner);
        var snack = CreateEntry(apple, Day, MealType.Snacks);
        var earlier = new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
        await using (var context = new NutrivoDbContext(options))
        {
            foreach (var (entry, time) in new[]
                     {
                         (snack, earlier), (breakfastLate, earlier.AddHours(2)),
                         (dinner, earlier), (lunchTwo, earlier),
                         (breakfastEarly, earlier.AddHours(1)), (lunchOne, earlier)
                     })
            {
                context.DiaryEntries.Add(entry);
                context.Entry(entry).Property<DateTimeOffset>("CreatedAt").CurrentValue =
                    time;
            }

            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var lunchIds = new[]
        {
            lunchOne.Id.Value, lunchTwo.Id.Value
        }.Order();
        var expected = new[]
            {
                breakfastEarly.Id.Value, breakfastLate.Id.Value
            }
            .Concat(lunchIds)
            .Concat([dinner.Id.Value, snack.Id.Value])
            .ToArray();

        var result = await GetAsync(client, Day);
        Assert.Equal(expected, result.Entries.Select(entry => entry.Id));
        AssertMealOrder(result);
        Assert.Equal(
            new DiaryNutritionTotalsResponse(900.03m, 60.03m, 6.03m, 30.03m),
            result.Totals
        );
        Assert.Equal(
            new[]
            {
                new DiaryNutritionTotalsResponse(300.01m, 20.01m, 2.01m, 10.01m),
                new DiaryNutritionTotalsResponse(300.01m, 20.01m, 2.01m, 10.01m),
                new DiaryNutritionTotalsResponse(150.01m, 10.01m, 1.01m, 5.01m),
                new DiaryNutritionTotalsResponse(150.01m, 10.01m, 1.01m, 5.01m)
            },
            result.MealTotals.Select(meal => meal.Totals)
        );
        Assert.Equal(
            expected,
            (await GetAsync(client, Day)).Entries.Select(entry => entry.Id)
        );
    }

    [Fact]
    public async Task
        Get_RetainsHistoricalFoodAndServingAfterCatalogueChangesAndDeletion()
    {
        var food = CreateFood("Original", brand: "Original brand");
        var options = await CreateDatabaseAsync(food);
        var direct = CreateEntry(food, Day);
        var serving = DiaryEntry.CreateFromServing(
                food,
                Day,
                MealType.Lunch,
                food.ServingSizes.Single().Id,
                0.5m
            )
            .Value;
        await using (var context = new NutrivoDbContext(options))
        {
            context.DiaryEntries.AddRange(direct, serving);
            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var original = await GetAsync(client, Day);
        await using (var context = new NutrivoDbContext(options))
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE public.foods SET name = 'Changed', brand = NULL, reference_quantity = 200,
                    kilocalories = 999, protein_grams = 0, carbohydrates_grams = 0, fat_grams = 0
                WHERE id = {food.Id.Value}
                """
            );
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE public.food_serving_sizes SET quantity = 3, unit = 'Changed', label = NULL,
                    equivalent_quantity = 999 WHERE food_id = {food.Id.Value}
                """
            );
        }

        var changed = await GetAsync(client, Day);
        Assert.Equal<DiaryDayEntryResponse>(original.Entries, changed.Entries);
        Assert.Equal(original.Totals, changed.Totals);
        Assert.Equal<DiaryMealTotalsResponse>(original.MealTotals, changed.MealTotals);
        await using (var context = new NutrivoDbContext(options))
        {
            await context.Foods.ExecuteDeleteAsync();
        }

        var deleted = await GetAsync(client, Day);
        Assert.Equal<DiaryDayEntryResponse>(original.Entries, deleted.Entries);
        Assert.Equal(original.Totals, deleted.Totals);
        Assert.Equal<DiaryMealTotalsResponse>(original.MealTotals, deleted.MealTotals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_SumsExactConsumptionBeforeRoundingEntriesAndMeals(
        bool differentMeals
    )
    {
        var nutrition = NutritionalValues.Create(0.005m, 0.005m, 0.005m, 0.005m).Value;
        var solid = Food.Create("Solid", null, 1m, ReferenceUnit.Gram, nutrition).Value;
        var liquid = Food.Create("Liquid", null, 1m, ReferenceUnit.Milliliter, nutrition)
            .Value;
        var options = await CreateDatabaseAsync(solid, liquid);
        await using (var context = new NutrivoDbContext(options))
        {
            context.DiaryEntries.AddRange(
                DiaryEntry.Create(solid, Day, MealType.Breakfast, 1m, ReferenceUnit.Gram)
                    .Value,
                DiaryEntry.Create(
                        liquid,
                        Day,
                        differentMeals ? MealType.Lunch : MealType.Breakfast,
                        1m,
                        ReferenceUnit.Milliliter
                    )
                    .Value
            );
            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var result = await GetAsync(client, Day);

        var rounded = new DiaryNutritionTotalsResponse(0.01m, 0.01m, 0.01m, 0.01m);
        Assert.Equal(rounded, result.Totals);
        Assert.Equal(2, result.Entries.Length);
        Assert.All(
            result.Entries,
            entry =>
            {
                Assert.Equal(0.01m, entry.Kilocalories);
                Assert.Equal(0.01m, entry.ProteinGrams);
                Assert.Equal(0.01m, entry.CarbohydratesGrams);
                Assert.Equal(0.01m, entry.FatGrams);
            }
        );
        AssertMealOrder(result);
        Assert.Equal(rounded, result.MealTotals[0].Totals);
        Assert.Equal(differentMeals ? rounded : ZeroTotals, result.MealTotals[1].Totals);
        Assert.Equal(ZeroTotals, result.MealTotals[2].Totals);
        Assert.Equal(ZeroTotals, result.MealTotals[3].Totals);
        await using var verification = new NutrivoDbContext(options);
        Assert.All(
            await verification.DiaryEntries.ToListAsync(),
            entry =>
            {
                Assert.Equal(nutrition, entry.NutritionalValues);
            }
        );
    }

    [Theory]
    [InlineData("Development", 0)]
    [InlineData("Development", 1)]
    [InlineData("Development", 2)]
    [InlineData("Development", 3)]
    [InlineData("Production", 0)]
    [InlineData("Production", 1)]
    [InlineData("Production", 2)]
    [InlineData("Production", 3)]
    public async Task Get_WhenDayTotalOverflows_Returns422WithoutChangingEntries(
        string environment,
        int nutrient
    )
    {
        decimal[] values = [0m, 0m, 0m, 0m];
        values[nutrient] = decimal.MaxValue;
        var nutrition = NutritionalValues.Create(
                values[0],
                values[1],
                values[2],
                values[3]
            )
            .Value;
        var food = Food.Create("Large", null, 1m, ReferenceUnit.Gram, nutrition).Value;
        var options = await CreateDatabaseAsync(food);
        var breakfast = DiaryEntry.Create(
                food,
                Day,
                MealType.Breakfast,
                1m,
                ReferenceUnit.Gram
            )
            .Value;
        var lunch = DiaryEntry.Create(
                food,
                Day,
                MealType.Lunch,
                1m,
                ReferenceUnit.Gram
            )
            .Value;
        await using (var context = new NutrivoDbContext(options))
        {
            context.DiaryEntries.AddRange(breakfast, lunch);
            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options, environment);
        using var client = CreateClient(factory);

        using var response =
            await client.GetAsync($"/diary-entries?date={Day:yyyy-MM-dd}");

        var problem = await AssertProblemAsync(
            response,
            HttpStatusCode.UnprocessableEntity
        );
        Assert.Equal(
            "GetDiaryDay.Totals.OutOfRange",
            problem.GetProperty("code").GetString()
        );
        Assert.False(problem.TryGetProperty("entries", out _));
        Assert.False(problem.TryGetProperty("totals", out _));
        Assert.False(problem.TryGetProperty("mealTotals", out _));
        Assert.DoesNotContain("OverflowException", problem.GetRawText());
        await using var verification = new NutrivoDbContext(options);
        var stored = await verification.DiaryEntries.ToListAsync();
        Assert.Equal(
            new[]
            {
                breakfast.Id.Value, lunch.Id.Value
            }.Order(),
            stored.Select(entry => entry.Id.Value).Order()
        );
        Assert.All(stored, entry => Assert.Equal(nutrition, entry.NutritionalValues));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Get_WithMissingOrInvalidDate_Returns400Problem(string environment)
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options, environment);
        using var client = CreateClient(factory);
        foreach (var query in new[]
                 {
                     "", "?date=", "?date=invalid", "?date=2026-02-30"
                 })
        {
            using var response = await client.GetAsync("/diary-entries" + query);
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Get_WhenStorageFails_ReturnsGeneric500(string environment)
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options, environment);
        await using var failingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddScoped<IDiaryQueries, FailingDiaryQueries>()
            )
        );
        using var client = CreateClient(failingFactory);

        using var response =
            await client.GetAsync($"/diary-entries?date={Day:yyyy-MM-dd}");

        var problem = await AssertProblemAsync(
            response,
            HttpStatusCode.InternalServerError
        );
        Assert.DoesNotContain("secret-storage-details", problem.GetRawText());
        Assert.DoesNotContain("InvalidOperationException", problem.GetRawText());
        Assert.False(problem.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task OpenApi_DescribesRequiredDateResponseSchemaAndStatusCodes()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var operation = document
            .GetProperty("paths")
            .GetProperty("/diary-entries")
            .GetProperty("get");
        Assert.Equal("GetDiaryDay", operation.GetProperty("operationId").GetString());
        Assert.Contains(
            operation.GetProperty("tags").EnumerateArray(),
            tag => tag.GetString() == "Diaries"
        );
        var parameter =
            Assert.Single(operation.GetProperty("parameters").EnumerateArray());
        Assert.Equal("date", parameter.GetProperty("name").GetString());
        Assert.Equal("query", parameter.GetProperty("in").GetString());
        Assert.True(parameter.GetProperty("required").GetBoolean());
        Assert.Equal(
            "date",
            parameter.GetProperty("schema").GetProperty("format").GetString()
        );
        foreach (var status in new[]
                 {
                     "200", "400", "422", "500"
                 })
        {
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
        }

        var schemas = document.GetProperty("components").GetProperty("schemas");
        var response =
            schemas.GetProperty("GetDiaryDayResponse").GetProperty("properties");
        Assert.True(response.TryGetProperty("date", out _));
        Assert.True(response.TryGetProperty("entries", out _));
        Assert.True(response.TryGetProperty("totals", out _));
        Assert.True(response.TryGetProperty("mealTotals", out _));
        var totals = schemas
            .GetProperty("DiaryNutritionTotalsResponse")
            .GetProperty("properties");
        foreach (var field in new[]
                 {
                     "kilocalories", "proteinGrams", "carbohydratesGrams", "fatGrams"
                 })
        {
            Assert.True(totals.TryGetProperty(field, out _));
        }

        var mealTotals = schemas
            .GetProperty("DiaryMealTotalsResponse")
            .GetProperty("properties");
        Assert.True(mealTotals.TryGetProperty("mealType", out _));
        Assert.True(mealTotals.TryGetProperty("totals", out _));
        var entry =
            schemas.GetProperty("DiaryDayEntryResponse").GetProperty("properties");
        foreach (var field in new[]
                 {
                     "id", "mealType", "foodId", "name", "brand", "quantity",
                     "referenceUnit", "kilocalories", "proteinGrams",
                     "carbohydratesGrams", "fatGrams", "servingCount", "servingSize"
                 })
        {
            Assert.True(entry.TryGetProperty(field, out _));
        }
    }

    [Fact]
    public async Task Query_PreservesPrecisionDoesNotTrackAndHonorsCancellation()
    {
        var food = CreateFood("Oats");
        var options = await CreateDatabaseAsync(food);
        await using (var context = new NutrivoDbContext(options))
        {
            context.DiaryEntries.Add(CreateEntry(food, Day));
            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options);
        using var scope = factory.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IDiaryQueries>();
        var db = scope.ServiceProvider.GetRequiredService<NutrivoDbContext>();

        var result = await query.GetDayAsync(Day, CancellationToken.None);

        Assert.Equal(150.005m, Assert.Single(result).Kilocalories);
        Assert.Empty(db.ChangeTracker.Entries());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            query.GetDayAsync(Day, cancellation.Token)
        );
    }

    private static void AssertMealOrder(GetDiaryDayResponse result)
    {
        Assert.Equal(
            new[]
            {
                MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snacks
            },
            result.MealTotals.Select(meal => meal.MealType)
        );
    }

    private async Task<DbContextOptions<NutrivoDbContext>> CreateDatabaseAsync(
        params Food[] foods
    )
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        context.Foods.AddRange(foods);
        await context.SaveChangesAsync();
        return options;
    }

    private static Food CreateFood(
        string name,
        ReferenceUnit unit = ReferenceUnit.Gram,
        string? brand = null
    )
    {
        return Food.Create(
                name,
                brand,
                100.123m,
                unit,
                NutritionalValues.Create(150.005m, 10.005m, 1.005m, 5.005m).Value,
                [ServingSize.Create(2m, "pieces", "Medium", 200.246m).Value]
            )
            .Value;
    }

    private static DiaryEntry CreateEntry(
        Food food,
        DateOnly date,
        MealType meal = MealType.Breakfast
    )
    {
        return DiaryEntry.Create(food, date, meal, 100.123m, food.ReferenceUnit).Value;
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            }
        );
    }

    private static async Task<GetDiaryDayResponse> GetAsync(
        HttpClient client,
        DateOnly date
    )
    {
        using var response =
            await client.GetAsync($"/diary-entries?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GetDiaryDayResponse>())!;
    }

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status
    )
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        return problem;
    }

    private sealed class FailingDiaryQueries : IDiaryQueries
    {
        public Task<Nutrivo.Application.Diaries.GetDiaryEntryById.DiaryEntryDetail?>
            GetByIdAsync(
                Nutrivo.Domain.Diaries.DiaryEntryId id,
                CancellationToken cancellationToken
            ) => throw new NotSupportedException();

        public Task<ImmutableArray<DiaryDayEntry>> GetDayAsync(
            DateOnly date,
            CancellationToken cancellationToken
        )
        {
            throw new InvalidOperationException("secret-storage-details");
        }
    }
}
