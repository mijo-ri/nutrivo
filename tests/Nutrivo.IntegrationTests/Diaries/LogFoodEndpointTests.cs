using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Api.Endpoints.Diaries.LogFood;
using Nutrivo.Application.Diaries;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Foods;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Diaries;

[Collection("PostgreSQL")]
public sealed class LogFoodEndpointTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(ReferenceUnit.Gram, false)]
    [InlineData(ReferenceUnit.Milliliter, false)]
    [InlineData(ReferenceUnit.Gram, true)]
    [InlineData(ReferenceUnit.Milliliter, true)]
    public async Task Post_WithValidInput_ReturnsCreatedAndCommitsEntry(
        ReferenceUnit unit,
        bool serving
    )
    {
        var (options, food) = await CreateDatabaseAsync(unit);
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = Payload(food);
        payload["mealType"] = serving ? 4 : 2;
        payload["date"] = serving ? "2030-01-01" : "2020-02-29";
        if (serving)
        {
            payload.Remove("quantity");
            payload.Remove("referenceUnit");
            payload["servingSizeId"] = food.ServingSizes.Single().Id.Value;
            payload["servingCount"] = 0.5m;
        }

        using var response = await client.PostAsJsonAsync("/diary-entries", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var body = (await response.Content.ReadFromJsonAsync<LogFoodResponse>())!;
        Assert.NotEqual(Guid.Empty, body.Id);
        await using var verification = new NutrivoDbContext(options);
        var entry = await verification.DiaryEntries.AsNoTracking().SingleAsync();
        Assert.Equal(body.Id, entry.Id.Value);
        Assert.Equal(food.Id, entry.FoodSnapshot.FoodId);
        Assert.Equal(food.Name, entry.FoodSnapshot.Name);
        Assert.Equal(food.NutritionalValues, entry.FoodSnapshot.NutritionalValues);
        Assert.Equal(
            serving ? new DateOnly(2030, 1, 1) : new DateOnly(2020, 2, 29),
            entry.Date
        );
        Assert.Equal(serving ? MealType.Snacks : MealType.Lunch, entry.MealType);
        Assert.Equal(unit, entry.ReferenceUnit);
        Assert.Equal(serving ? 58m : 25m, entry.Quantity);
        Assert.Equal(
            serving ? 174.0058m : 75.0025m,
            entry.NutritionalValues.Kilocalories
        );
        if (serving)
        {
            Assert.Equal(
                food.ServingSizes.Single().Id,
                entry.ServingSizeSnapshot!.ServingSizeId
            );
            Assert.Equal(0.5m, entry.ServingCount);
        }
        else
        {
            Assert.Null(entry.ServingSizeSnapshot);
            Assert.Null(entry.ServingCount);
        }
    }

    [Fact]
    public async Task Post_WithUnknownFood_Returns404AndSavesNothing()
    {
        var (options, food) = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = Payload(food);
        payload["foodId"] = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync("/diary-entries", payload);

        await AssertProblemAsync(
            response,
            HttpStatusCode.NotFound,
            "LogFood.Food.NotFound"
        );
        await AssertNoEntriesAsync(options);
    }

    [Fact]
    public async Task Post_WithIncompleteOrMixedAmounts_ReturnsStable400AndSavesNothing()
    {
        var (options, food) = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        foreach (var mask in Enumerable.Range(0, 16).Except([3, 12]))
        {
            var payload = Payload(food);
            payload["quantity"] = (mask & 1) != 0 ? JsonValue.Create(25m) : null;
            payload["referenceUnit"] = (mask & 2) != 0 ? JsonValue.Create(1) : null;
            payload["servingSizeId"] = (mask & 4) != 0
                ? JsonValue.Create(food.ServingSizes.Single().Id.Value)
                : null;
            payload["servingCount"] = (mask & 8) != 0 ? JsonValue.Create(1m) : null;

            using var response = await client.PostAsJsonAsync("/diary-entries", payload);

            await AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "LogFood.Amount.Invalid"
            );
        }

        await AssertNoEntriesAsync(options);
    }

    public static TheoryData<string, string, string> InvalidDomainInputs
    {
        get
        {
            return new TheoryData<string, string, string>
            {
                {
                    "foodId", "\"00000000-0000-0000-0000-000000000000\"", "Food.Id.Empty"
                },
                {
                    "quantity", "0", "DiaryEntry.Quantity.Invalid"
                },
                {
                    "quantity", "-1", "DiaryEntry.Quantity.Invalid"
                },
                {
                    "referenceUnit", "0", "DiaryEntry.ReferenceUnit.Invalid"
                },
                {
                    "referenceUnit", "2", "DiaryEntry.ReferenceUnit.Mismatch"
                },
                {
                    "mealType", "0", "DiaryEntry.MealType.Invalid"
                },
                {
                    "mealType", "5", "DiaryEntry.MealType.Invalid"
                },
                {
                    "quantity", "79228162514264337593543950335",
                    "DiaryEntry.Nutrition.OutOfRange"
                },
                {
                    "servingSizeId", "\"00000000-0000-0000-0000-000000000000\"",
                    "ServingSize.Id.Empty"
                },
                {
                    "servingSizeId", "\"11111111-1111-1111-1111-111111111111\"",
                    "DiaryEntry.ServingSize.NotFound"
                },
                {
                    "servingCount", "0", "DiaryEntry.ServingCount.Invalid"
                },
                {
                    "servingCount", "-0.5", "DiaryEntry.ServingCount.Invalid"
                },
                {
                    "servingCount", "79228162514264337593543950335",
                    "DiaryEntry.Quantity.OutOfRange"
                }
            };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidDomainInputs))]
    public async Task Post_WithDomainFailure_ReturnsStable400AndSavesNothing(
        string property,
        string value,
        string code
    )
    {
        var (options, food) = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = Payload(food);
        if (property.StartsWith("serving", StringComparison.Ordinal))
        {
            payload.Remove("quantity");
            payload.Remove("referenceUnit");
            payload["servingSizeId"] = food.ServingSizes.Single().Id.Value;
            payload["servingCount"] = 1m;
        }

        payload[property] = JsonNode.Parse(value);

        using var response = await client.PostAsJsonAsync("/diary-entries", payload);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, code);
        await AssertNoEntriesAsync(options);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Post_WithBindingFailures_Returns400ProblemAndSavesNothing(
        string environment
    )
    {
        var (options, food) = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options, environment);
        using var client = CreateClient(factory);
        var invalid = new List<string> { "{ invalid json", "null", "" };
        foreach (var field in new[] { "foodId", "date", "mealType" })
        {
            var missing = Payload(food);
            missing.Remove(field);
            invalid.Add(missing.ToJsonString());
            var nullField = Payload(food);
            nullField[field] = null;
            invalid.Add(nullField.ToJsonString());
        }

        foreach (var (field, value) in new[]
                 {
                     ("foodId", "\"invalid-uuid\""), ("date", "\"2026-02-30\""),
                     ("date", "\"2026-10-29T10:00:00Z\""),
                     ("mealType", "\"invalid-meal\""),
                     ("quantity", "\"not a number\""), ("referenceUnit", "\"grams\""),
                     ("servingSizeId", "\"invalid-uuid\""),
                     ("servingCount", "\"invalid-count\"")
                 })
        {
            var payload = Payload(food);
            payload[field] = JsonNode.Parse(value);
            invalid.Add(payload.ToJsonString());
        }

        foreach (var json in invalid)
        {
            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );
            using var response = await client.PostAsync("/diary-entries", content);
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        }

        await AssertNoEntriesAsync(options);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Post_WhenStorageFails_ReturnsGeneric500(string environment)
    {
        var (options, food) = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options, environment);
        await using var failingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddScoped<IDiaryEntryRepository, FailingDiaryEntryRepository>()
            )
        );
        using var client = CreateClient(failingFactory);

        using var response =
            await client.PostAsJsonAsync("/diary-entries", Payload(food));

        var problem = await AssertProblemAsync(
            response,
            HttpStatusCode.InternalServerError
        );
        Assert.DoesNotContain("secret-storage-details", problem.GetRawText());
        Assert.DoesNotContain("InvalidOperationException", problem.GetRawText());
        Assert.False(problem.TryGetProperty("code", out _));
        await AssertNoEntriesAsync(options);
    }

    [Fact]
    public async Task OpenApi_DescribesLogFoodRequestAndResponses()
    {
        var (options, _) = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var operation = document
            .GetProperty("paths")
            .GetProperty("/diary-entries")
            .GetProperty("post");
        Assert.Equal("LogFood", operation.GetProperty("operationId").GetString());
        Assert.Contains(
            operation.GetProperty("tags").EnumerateArray(),
            tag => tag.GetString() == "Diaries"
        );
        foreach (var status in new[] { "201", "400", "404", "500" })
        {
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
        }

        var description = operation.GetProperty("description").GetString();
        Assert.Contains("\"quantity\": 75", description);
        Assert.Contains("\"servingCount\": 0.5", description);
        var schema = document
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("LogFoodRequest");
        Assert.Equal(
            new[] { "foodId", "date", "mealType" },
            schema
                .GetProperty("required")
                .EnumerateArray()
                .Select(field => field.GetString())
        );
        Assert.Equal(
            "date",
            schema
                .GetProperty("properties")
                .GetProperty("date")
                .GetProperty("format")
                .GetString()
        );
        foreach (var field in new[]
                     { "quantity", "referenceUnit", "servingSizeId", "servingCount" })
        {
            Assert.True(schema.GetProperty("properties").TryGetProperty(field, out _));
        }
    }

    private static JsonObject Payload(Food food)
    {
        return new JsonObject
        {
            ["foodId"] = food.Id.Value,
            ["date"] = "2026-10-29",
            ["mealType"] = 1,
            ["quantity"] = 25m,
            ["referenceUnit"] = (int)food.ReferenceUnit
        };
    }

    private async Task<(DbContextOptions<NutrivoDbContext> Options, Food Food)>
        CreateDatabaseAsync(ReferenceUnit unit = ReferenceUnit.Gram)
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        var food = Food.Create(
                "Egg",
                "Brand",
                50m,
                unit,
                NutritionalValues.Create(150.005m, 10.005m, 1.005m, 5.005m).Value,
                [ServingSize.Create(2m, "pieces", "Medium", 116m).Value]
            )
            .Value;
        context.Foods.Add(food);
        await context.SaveChangesAsync();
        return (options, food);
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

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string? code = null
    )
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        if (code is not null)
        {
            Assert.Equal(code, problem.GetProperty("code").GetString());
            Assert.False(
                string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString())
            );
        }

        return problem;
    }

    private static async Task AssertNoEntriesAsync(
        DbContextOptions<NutrivoDbContext> options
    )
    {
        await using var context = new NutrivoDbContext(options);
        Assert.False(await context.DiaryEntries.AnyAsync());
        Assert.Single(await context.Foods.ToListAsync());
    }

    private sealed class FailingDiaryEntryRepository : IDiaryEntryRepository
    {
        public Task AddAsync(DiaryEntry entry, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("secret-storage-details");
        }
    }
}
