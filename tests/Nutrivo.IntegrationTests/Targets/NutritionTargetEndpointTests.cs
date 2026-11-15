using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Application.Targets;
using Nutrivo.Domain.Targets;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Foods;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Targets;

[Collection("PostgreSQL")]
public sealed class NutritionTargetEndpointTests(PostgreSqlFixture fixture)
{
    private static readonly string[] Fields =
        ["kilocalories", "proteinGrams", "carbohydratesGrams", "fatGrams"];

    private static HttpClient Client(FoodApiFactory factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            }
        );
    }

    private async Task<DbContextOptions<NutrivoDbContext>> Database()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var db = new NutrivoDbContext(options);
        await db.Database.MigrateAsync();
        return options;
    }

    private static DailyNutritionTargetValues Values(decimal kcal = 2400.123456789m)
    {
        return new DailyNutritionTargetValues(kcal, 180.5m, 260.25m, 70.125m);
    }

    [Fact]
    public async Task UnsetAndSavedTargets_SurviveNewHostAndAcceptZero()
    {
        var options = await Database();
        await using (var factory = new FoodApiFactory(options))
        {
            using var client = Client(factory);
            Assert.Null(
                (await client.GetFromJsonAsync<DailyNutritionTargetResult>(
                    "/nutrition-targets"
                ))!.Targets
            );
            using var response =
                await client.PutAsJsonAsync("/nutrition-targets", Values());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                Values(),
                (await response.Content.ReadFromJsonAsync<DailyNutritionTargetResult>())!
                .Targets
            );
        }

        await using var restarted = new FoodApiFactory(options);
        using var fresh = Client(restarted);
        Assert.Equal(
            Values(),
            (await fresh.GetFromJsonAsync<DailyNutritionTargetResult>(
                "/nutrition-targets"
            ))!.Targets
        );
        using var zero = await fresh.PutAsJsonAsync(
            "/nutrition-targets",
            new DailyNutritionTargetValues(0, 0, 0, 0)
        );
        Assert.Equal(HttpStatusCode.OK, zero.StatusCode);
        Assert.Equal(
            0m,
            (await fresh.GetFromJsonAsync<DailyNutritionTargetResult>(
                "/nutrition-targets"
            ))!.Targets!.Kilocalories
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidFields_LeaveExistingOrUnsetStateUnchanged(bool existing)
    {
        var options = await Database();
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        if (existing)
        {
            (await client.PutAsJsonAsync("/nutrition-targets", Values()))
                .EnsureSuccessStatusCode();
        }

        foreach (var field in Fields)
        foreach (var invalid in new[]
                 {
                     "missing", "null", "-1", "\"bad\"", "1e100"
                 })
        {
            var payload = JsonSerializer.SerializeToNode(
                Values(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            )!.AsObject();
            if (invalid == "missing")
            {
                payload.Remove(field);
            }
            else
            {
                payload[field] = JsonNode.Parse(invalid);
            }

            using var response = await client.PutAsync(
                "/nutrition-targets",
                new StringContent(
                    payload.ToJsonString(),
                    Encoding.UTF8,
                    "application/json"
                )
            );
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType!.MediaType
            );
            if (invalid == "-1")
            {
                var error = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(
                    $"DailyNutritionTarget.{char.ToUpperInvariant(field[0])}{field[1..]}.Negative",
                    error.GetProperty("code").GetString()
                );
            }

            var stored =
                (await client.GetFromJsonAsync<DailyNutritionTargetResult>(
                    "/nutrition-targets"
                ))!.Targets;
            Assert.Equal(existing ? Values() : null, stored);
        }
    }

    [Fact]
    public async Task ConcurrentPut_ReturnsCommittedSetsAndStoresOneWholeSet()
    {
        var options = await Database();
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        var candidates = Enumerable
            .Range(1, 8)
            .Select(i => new DailyNutritionTargetValues(i, i * 2, i * 3, i * 4))
            .ToArray();
        await Task.WhenAll(
            candidates.Select(async values =>
                {
                    using var response = await client.PutAsJsonAsync(
                        "/nutrition-targets",
                        values
                    );
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(
                        values,
                        (await response.Content
                            .ReadFromJsonAsync<DailyNutritionTargetResult>())!.Targets
                    );
                }
            )
        );
        Assert.Contains(
            (await client.GetFromJsonAsync<DailyNutritionTargetResult>(
                "/nutrition-targets"
            ))!.Targets!,
            candidates
        );
        await using var db = new NutrivoDbContext(options);
        Assert.Equal(1, await db.Set<DailyNutritionTarget>().CountAsync());
    }

    [Fact]
    public async Task StorageFailure_ReturnsGenericProblemAndPreservesSet()
    {
        var options = await Database();
        await using var factory = new FoodApiFactory(options, "Production");
        using var client = Client(factory);
        (await client.PutAsJsonAsync("/nutrition-targets", Values()))
            .EnsureSuccessStatusCode();
        await using (var db = new NutrivoDbContext(options))
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE FUNCTION reject_targets() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'secret-target-storage'; END; $$"
            );
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_targets BEFORE INSERT OR UPDATE ON public.daily_nutrition_targets FOR EACH ROW EXECUTE FUNCTION reject_targets()"
            );
        }

        using var response = await client.PutAsJsonAsync("/nutrition-targets", Values(1));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain(
            "secret-target-storage",
            await response.Content.ReadAsStringAsync()
        );
        Assert.Equal(
            Values(),
            (await client.GetFromJsonAsync<DailyNutritionTargetResult>(
                "/nutrition-targets"
            ))!.Targets
        );
    }

    [Fact]
    public async Task OpenApi_DescribesTargetContracts()
    {
        var options = await Database();
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        var api = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var path = api.GetProperty("paths").GetProperty("/nutrition-targets");
        foreach (var operation in new[]
                 {
                     "get", "put"
                 })
        {
            var responses = path.GetProperty(operation).GetProperty("responses");
            Assert.True(responses.TryGetProperty("200", out _));
            Assert.True(responses.TryGetProperty("500", out _));
        }

        Assert.True(
            path.GetProperty("put").GetProperty("responses").TryGetProperty("400", out _)
        );
        var schemas = api.GetProperty("components").GetProperty("schemas");
        var request = schemas.GetProperty("SetNutritionTargetRequest");
        var required = request
            .GetProperty("required")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        foreach (var field in Fields)
        {
            Assert.Contains(field, required);
        }

        var responseSchema = schemas
            .GetProperty("DailyNutritionTargetResult")
            .GetProperty("properties")
            .GetProperty("targets")
            .ToString();
        Assert.Contains("null", responseSchema);
    }
}
