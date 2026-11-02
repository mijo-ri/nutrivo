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
        Assert.False(json.TryGetProperty("totals", out _));
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
        Assert.Empty((await GetAsync(client, date.AddDays(2))).Entries);
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

        Assert.Equal(
            expected,
            (await GetAsync(client, Day)).Entries.Select(entry => entry.Id)
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

        Assert.Equal<DiaryDayEntryResponse>(
            original.Entries,
            (await GetAsync(client, Day)).Entries
        );
        await using (var context = new NutrivoDbContext(options))
        {
            await context.Foods.ExecuteDeleteAsync();
        }

        Assert.Equal<DiaryDayEntryResponse>(
            original.Entries,
            (await GetAsync(client, Day)).Entries
        );
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
                     "200", "400", "500"
                 })
        {
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
        }

        var schemas = document.GetProperty("components").GetProperty("schemas");
        var response =
            schemas.GetProperty("GetDiaryDayResponse").GetProperty("properties");
        Assert.True(response.TryGetProperty("date", out _));
        Assert.True(response.TryGetProperty("entries", out _));
        Assert.False(response.TryGetProperty("totals", out _));
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

        Assert.Equal(150.005m, Assert.Single(result.Entries).Kilocalories);
        Assert.Empty(db.ChangeTracker.Entries());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            query.GetDayAsync(Day, cancellation.Token)
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
        public Task<GetDiaryDayResult> GetDayAsync(
            DateOnly date,
            CancellationToken cancellationToken
        )
        {
            throw new InvalidOperationException("secret-storage-details");
        }
    }
}
