using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Foods;

[Collection("PostgreSQL")]
public sealed class DeleteFoodEndpointTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_RemovesCatalogueAndPreservesDiaryAcrossDays(bool used)
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture, used);
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        using var before =
            JsonDocument.Parse(await DeleteFoodTestData.StateAsync(options));
        var days = new[]
        {
            "2026-11-10", "2026-11-11"
        };
        var diary = await Task.WhenAll(
            days.Select(day => client.GetStringAsync($"/diary-entries?date={day}"))
        );
        using var response = await client.DeleteAsync($"/foods/{food.Id.Value}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        using var after =
            JsonDocument.Parse(await DeleteFoodTestData.StateAsync(options));
        Assert.Equal(
            before.RootElement.GetProperty("diary").GetRawText(),
            after.RootElement.GetProperty("diary").GetRawText()
        );
        Assert.Equal(
            diary,
            await Task.WhenAll(
                days.Select(day => client.GetStringAsync($"/diary-entries?date={day}"))
            )
        );
        // Compare every remaining row to its original, including unrelated servings.
        foreach (var table in new[]
                 {
                     "foods", "servings"
                 })
        {
            var remaining =
                after.RootElement.GetProperty(table).EnumerateArray().ToArray();
            Assert.Equal(table == "foods" ? 1 : 2, remaining.Length);
            foreach (var row in remaining)
            {
                Assert.Contains(
                    before.RootElement.GetProperty(table).EnumerateArray(),
                    old => old.GetRawText() == row.GetRawText()
                );
            }
        }

        await using var read = new NutrivoDbContext(options);
        Assert.False(await read.Foods.AnyAsync(f => f.Id == food.Id));
        Assert.False(
            await read
                .Set<ServingSize>()
                .AnyAsync(s => EF.Property<FoodId>(s, "FoodId") == food.Id)
        );
    }

    [Theory]
    [InlineData("not-a-guid", 400, null)]
    [InlineData("00000000-0000-0000-0000-000000000000", 400, "Food.Id.Empty")]
    [InlineData("11111111-1111-1111-1111-111111111111", 404, "DeleteFood.NotFound")]
    public async Task Delete_InvalidOrMissingTargetPreservesAllData(
        string id,
        int status,
        string? code
    )
    {
        var (options, _) = await DeleteFoodTestData.SeedAsync(fixture);
        var before = await DeleteFoodTestData.StateAsync(options);
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        using var response = await client.DeleteAsync($"/foods/{id}");
        await AssertProblemAsync(response, status, code);
        Assert.Equal(before, await DeleteFoodTestData.StateAsync(options));
    }

    [Fact]
    public async Task Delete_RepeatedOrConcurrentRequestsReturnNotFoundAfterOneSuccess()
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture);
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var responses = await Task.WhenAll(
            client.DeleteAsync($"/foods/{food.Id.Value}"),
            client.DeleteAsync($"/foods/{food.Id.Value}")
        );
        try
        {
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
            await AssertProblemAsync(
                Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NotFound),
                404,
                "DeleteFood.NotFound"
            );
            var state = await DeleteFoodTestData.StateAsync(options);
            using var repeated = await client.DeleteAsync($"/foods/{food.Id.Value}");
            await AssertProblemAsync(repeated, 404, "DeleteFood.NotFound");
            Assert.Equal(state, await DeleteFoodTestData.StateAsync(options));
            await using var read = new NutrivoDbContext(options);
            Assert.False(await read.Foods.AnyAsync(f => f.Id == food.Id));
            Assert.Equal(2, await read.Set<ServingSize>().CountAsync());
            Assert.Equal(6, await read.DiaryEntries.CountAsync());
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Delete_FailedCascadeReturnsGeneric500AndRollsBack(
        string environment
    )
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture);
        var before = await DeleteFoodTestData.StateAsync(options);
        await DeleteFoodTestData.RejectServingDeletionAsync(options);
        await using var factory = new FoodApiFactory(options, environment);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            }
        );
        using var response = await client.DeleteAsync($"/foods/{food.Id.Value}");
        await AssertProblemAsync(response, 500, null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-storage-details", body);
        Assert.DoesNotContain("PostgresException", body);
        Assert.DoesNotContain("reject_serving_delete", body);
        Assert.Equal(before, await DeleteFoodTestData.StateAsync(options));
    }

    [Fact]
    public async Task
        DeletedFood_IsUnavailableForSearchDetailUpdateAndBothLoggingAmounts()
    {
        var (options, food) = await DeleteFoodTestData.SeedAsync(fixture);
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        using var searchBefore =
            JsonDocument.Parse(await client.GetStringAsync("/foods"));
        var count = searchBefore.RootElement.GetProperty("totalCount").GetInt32();
        using var deletion = await client.DeleteAsync($"/foods/{food.Id.Value}");
        Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
        var state = await DeleteFoodTestData.StateAsync(options);
        using var search = JsonDocument.Parse(await client.GetStringAsync("/foods"));
        Assert.Equal(count - 1, search.RootElement.GetProperty("totalCount").GetInt32());
        Assert.DoesNotContain(
            search.RootElement.GetProperty("items").EnumerateArray(),
            f => f.GetProperty("id").GetGuid() == food.Id.Value
        );
        using var filtered =
            JsonDocument.Parse(await client.GetStringAsync("/foods?search=Target"));
        Assert.Empty(filtered.RootElement.GetProperty("items").EnumerateArray());
        using var detail = await client.GetAsync($"/foods/{food.Id.Value}");
        await AssertProblemAsync(detail, 404, "GetFoodById.NotFound");
        using var update = await client.PutAsJsonAsync(
            $"/foods/{food.Id.Value}",
            new
            {
                name = "Updated",
                referenceQuantity = 100m,
                referenceUnit = 1,
                kilocalories = 100m,
                proteinGrams = 1m,
                carbohydratesGrams = 2m,
                fatGrams = 3m
            }
        );
        await AssertProblemAsync(update, 404, "UpdateFood.NotFound");
        foreach (var serving in new[]
                 {
                     false, true
                 })
        {
            object amount = serving
                ? new
                {
                    foodId = food.Id.Value,
                    date = "2026-11-11",
                    mealType = 1,
                    servingSizeId = food.ServingSizes[0].Id.Value,
                    servingCount = 1m
                }
                : new
                {
                    foodId = food.Id.Value,
                    date = "2026-11-11",
                    mealType = 1,
                    quantity = 50m,
                    referenceUnit = 1
                };
            using var logged = await client.PostAsJsonAsync("/diary-entries", amount);
            await AssertProblemAsync(logged, 404, "LogFood.Food.NotFound");
        }

        Assert.Equal(state, await DeleteFoodTestData.StateAsync(options));
    }

    [Fact]
    public async Task OpenApi_DescribesPermanentDeletionAndHistoryPreservation()
    {
        var (options, _) = await DeleteFoodTestData.SeedAsync(fixture);
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var operation = document
            .GetProperty("paths")
            .GetProperty("/foods/{id}")
            .GetProperty("delete");
        Assert.Equal("DeleteFood", operation.GetProperty("operationId").GetString());
        Assert.Contains(
            operation.GetProperty("tags").EnumerateArray(),
            tag => tag.GetString() == "Foods"
        );
        Assert.Contains("snapshots", operation.GetProperty("description").GetString());
        Assert.Contains("Permanently", operation.GetProperty("summary").GetString());
        Assert.False(operation.TryGetProperty("requestBody", out _));
        var parameter =
            Assert.Single(operation.GetProperty("parameters").EnumerateArray());
        Assert.Equal("id", parameter.GetProperty("name").GetString());
        Assert.Equal("path", parameter.GetProperty("in").GetString());
        Assert.True(parameter.GetProperty("required").GetBoolean());
        Assert.Equal(
            "uuid",
            parameter.GetProperty("schema").GetProperty("format").GetString()
        );
        foreach (var status in new[]
                 {
                     "204", "400", "404", "500"
                 })
        {
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
        }

        Assert.False(
            operation
                .GetProperty("responses")
                .GetProperty("204")
                .TryGetProperty("content", out _)
        );
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        int status,
        string? code
    )
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        if (code is not null)
        {
            Assert.Equal(code, problem.GetProperty("code").GetString());
        }
    }
}
