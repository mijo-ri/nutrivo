using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Api.Endpoints.Foods;
using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.SearchFoods;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Foods;

[Collection("PostgreSQL")]
public sealed class SearchFoodsEndpointTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData("oAt", "Rolled Oats")]
    [InlineData("FARM", "Milk")]
    [InlineData("  oat \t", "Rolled Oats")]
    [InlineData("m", "Milk")]
    [InlineData("oats farm", null)]
    [InlineData("missing", null)]
    public async Task Get_MatchesLiteralSubstringOfNameOrBrand(
        string search,
        string? expectedName
    )
    {
        var options = await CreateDatabaseAsync(
            CreateFood("Rolled Oats"),
            CreateFood("Milk", "Happy Farm")
        );
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var result = await GetAsync(
            client,
            $"/foods?search={Uri.EscapeDataString(search)}"
        );

        Assert.Equal(expectedName is null ? 0 : 1, result.TotalCount);
        if (expectedName is null)
        {
            Assert.Empty(result.Items);
        }
        else
        {
            Assert.Equal(expectedName, Assert.Single(result.Items).Name);
        }
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    [InlineData("\\%_")]
    [InlineData("' OR 1=1 --")]
    public async Task Get_TreatsWildcardsEscapeAndSqlTextLiterally(string search)
    {
        var nameMatch = CreateFood($"Food {search} special");
        var brandMatch = CreateFood("Branded food", $"Brand {search} special");
        var options = await CreateDatabaseAsync(
            nameMatch,
            brandMatch,
            CreateFood("Plain food")
        );
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var result = await GetAsync(
            client,
            $"/foods?search={Uri.EscapeDataString(search)}"
        );

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(
            new[] { brandMatch.Id.Value, nameMatch.Id.Value },
            result.Items.Select(item => item.Id)
        );
    }

    [Theory]
    [InlineData("/foods")]
    [InlineData("/foods?search=")]
    [InlineData("/foods?search=%20%09%20")]
    public async Task Get_WithEmptySearch_ListsFoodsUsingDefaultPage(string url)
    {
        var options = await CreateDatabaseAsync(
            Enumerable
                .Range(1, 21)
                .Select(index => CreateFood($"Food {index:D2}"))
                .ToArray()
        );
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var result = await GetAsync(client, url);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(21, result.TotalCount);
        Assert.Equal(20, result.Items.Length);
        Assert.Equal("Food 01", result.Items[0].Name);
        Assert.Equal("Food 20", result.Items[^1].Name);
    }

    [Fact]
    public async Task Get_PaginatesFilteredFoodsWithStableCaseInsensitiveOrdering()
    {
        var duplicateOne = CreateFood("Oats", "alpha");
        var duplicateTwo = CreateFood("Oats", "ALPHA");
        var nullBrand = CreateFood("oats");
        var lastBrand = CreateFood("OATS", "Zeta");
        var lastName = CreateFood("Zebra oats");
        var duplicates = new[] { duplicateOne, duplicateTwo }
            .OrderBy(food => food.Id.Value)
            .ToArray();
        var expected = new[]
            { nullBrand, duplicates[0], duplicates[1], lastBrand, lastName };
        var options = await CreateDatabaseAsync(
            lastName,
            lastBrand,
            duplicateTwo,
            nullBrand,
            duplicateOne,
            CreateFood("Apple")
        );
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var ids = new List<Guid>();

        for (var page = 1; page <= 3; page++)
        {
            var result = await GetAsync(
                client,
                $"/foods?search=oats&page={page}&pageSize=2"
            );
            Assert.Equal(page, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(page == 3 ? 1 : 2, result.Items.Length);
            ids.AddRange(result.Items.Select(item => item.Id));
        }

        Assert.Equal(expected.Select(food => food.Id.Value), ids);
        var beyond = await GetAsync(client, "/foods?search=oats&page=4&pageSize=2");
        Assert.Empty(beyond.Items);
        Assert.Equal(5, beyond.TotalCount);
        var largestPage = await GetAsync(
            client,
            $"/foods?page={int.MaxValue}&pageSize=1"
        );
        Assert.Empty(largestPage.Items);
        Assert.Equal(6, largestPage.TotalCount);
    }

    [Fact]
    public async Task Get_WithEmptyDatabase_ReturnsEmptyPage()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var result = await GetAsync(client, "/foods");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram, null)]
    [InlineData(ReferenceUnit.Milliliter, "Example")]
    public async Task Get_ReturnsCompleteNutritionOfNewlyCreatedFood(
        ReferenceUnit unit,
        string? brand
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        using var created = await client.PostAsJsonAsync(
            "/foods",
            new
            {
                name = "Oats",
                brand,
                referenceUnit = unit,
                kilocalories = 372.123456789m,
                proteinGrams = 13.567891234m,
                carbohydratesGrams = 58.765432198m,
                fatGrams = 7.012345678m
            }
        );
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        using var response = await client.GetAsync("/foods?search=oats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, json.EnumerateObject().Count());
        var item = Assert.Single(json.GetProperty("items").EnumerateArray());
        Assert.Equal(9, item.EnumerateObject().Count());
        Assert.Equal(id, item.GetProperty("id").GetGuid());
        Assert.Equal("Oats", item.GetProperty("name").GetString());
        Assert.Equal(brand, item.GetProperty("brand").GetString());
        Assert.Equal(100m, item.GetProperty("referenceQuantity").GetDecimal());
        Assert.Equal((int)unit, item.GetProperty("referenceUnit").GetInt32());
        Assert.Equal(372.123456789m, item.GetProperty("kilocalories").GetDecimal());
        Assert.Equal(13.567891234m, item.GetProperty("proteinGrams").GetDecimal());
        Assert.Equal(58.765432198m, item.GetProperty("carbohydratesGrams").GetDecimal());
        Assert.Equal(7.012345678m, item.GetProperty("fatGrams").GetDecimal());
    }

    [Theory]
    [InlineData("page=0", "SearchFoods.Page.Invalid")]
    [InlineData("page=-1", "SearchFoods.Page.Invalid")]
    [InlineData("pageSize=0", "SearchFoods.PageSize.Invalid")]
    [InlineData("pageSize=-1", "SearchFoods.PageSize.Invalid")]
    [InlineData("pageSize=101", "SearchFoods.PageSize.Invalid")]
    [InlineData("page=2147483647&pageSize=20", "SearchFoods.Page.Invalid")]
    [InlineData("page=abc", null)]
    [InlineData("pageSize=1.5", null)]
    [InlineData("page=2147483648", null)]
    public async Task Get_WithInvalidPagination_ReturnsProblem(string query, string? code)
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/foods?{query}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        if (code is not null)
        {
            Assert.Equal(code, problem.GetProperty("code").GetString());
            Assert.False(
                string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString())
            );
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
                services.AddScoped<IFoodQueries, FailingFoodQueries>()
            )
        );
        using var client = CreateClient(failingFactory);

        using var response = await client.GetAsync("/foods");

        var problem = await AssertProblemAsync(
            response,
            HttpStatusCode.InternalServerError
        );
        Assert.DoesNotContain("secret-storage-details", problem.GetRawText());
        Assert.DoesNotContain("InvalidOperationException", problem.GetRawText());
        Assert.False(problem.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task OpenApi_DescribesSearchParametersDefaultsAndResponses()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var operation =
            document.GetProperty("paths").GetProperty("/foods").GetProperty("get");
        Assert.Equal("SearchFoods", operation.GetProperty("operationId").GetString());
        var parameters = operation
            .GetProperty("parameters")
            .EnumerateArray()
            .ToDictionary(parameter => parameter.GetProperty("name").GetString()!);
        Assert.Equal(3, parameters.Count);
        foreach (var parameter in parameters.Values)
        {
            Assert.Equal("query", parameter.GetProperty("in").GetString());
            Assert.False(
                parameter.TryGetProperty("required", out var required)
                && required.GetBoolean()
            );
            Assert.False(
                string.IsNullOrWhiteSpace(
                    parameter.GetProperty("description").GetString()
                )
            );
        }

        Assert.Equal(
            1,
            parameters["page"].GetProperty("schema").GetProperty("default").GetInt32()
        );
        Assert.Equal(
            20,
            parameters["pageSize"].GetProperty("schema").GetProperty("default").GetInt32()
        );
        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("500", out _));
    }

    [Fact]
    public async Task Search_DoesNotTrackFoodsAndHonorsCancellation()
    {
        var options = await CreateDatabaseAsync(CreateFood("Oats"));
        await using var factory = new FoodApiFactory(options);
        using var scope = factory.Services.CreateScope();
        var search = scope.ServiceProvider.GetRequiredService<IFoodQueries>();
        var context = scope.ServiceProvider.GetRequiredService<NutrivoDbContext>();

        var result = await search.SearchAsync("oats", 1, 100, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Empty(context.ChangeTracker.Entries());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            search.SearchAsync(null, 1, 20, cancellation.Token)
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

    private static Food CreateFood(string name, string? brand = null)
    {
        return Food.Create(
                name,
                brand,
                ReferenceUnit.Gram,
                NutritionalValues.Create(100m, 10m, 20m, 1m).Value
            )
            .Value;
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

    private static async Task<SearchFoodsResponse> GetAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SearchFoodsResponse>())!;
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

    private sealed class FailingFoodQueries : IFoodQueries
    {
        public Task<SearchFoodsResult> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            throw new InvalidOperationException("secret-storage-details");
        }
    }
}
