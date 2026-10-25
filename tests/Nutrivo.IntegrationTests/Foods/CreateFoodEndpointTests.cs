using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Application.Foods;
using Nutrivo.Api.Endpoints.Foods.SearchFoods;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Foods;

[Collection("PostgreSQL")]
public sealed class CreateFoodEndpointTests(PostgreSqlFixture fixture)
{
    private const string ValidJson = """
        {
          "name": "  Rolled  oats  ",
          "brand": " Example  brand ",
          "referenceQuantity": 100,
          "referenceUnit": 1,
          "kilocalories": 372.123456789,
          "proteinGrams": 13.567891234,
          "carbohydratesGrams": 58.765432198,
          "fatGrams": 7.012345678
        }
        """;

    [Theory]
    [InlineData(ReferenceUnit.Gram, true)]
    [InlineData(ReferenceUnit.Milliliter, false)]
    public async Task Post_WithValidFood_ReturnsIdOfCommittedFood(
        ReferenceUnit unit,
        bool includeBrand
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = JsonNode.Parse(ValidJson)!.AsObject();
        payload["referenceUnit"] = (int)unit;

        if (!includeBrand)
        {
            payload.Remove("brand");
        }

        using var response = await client.PostAsJsonAsync("/foods", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(body.EnumerateObject());
        var id = FoodId.Create(body.GetProperty("id").GetGuid()).Value;
        await using var context = new NutrivoDbContext(options);
        var saved = await context.Foods.AsNoTracking().SingleAsync(food => food.Id == id);
        Assert.Equal("Rolled  oats", saved.Name);
        Assert.Equal(includeBrand ? "Example  brand" : null, saved.Brand);
        Assert.Equal(unit, saved.ReferenceUnit);
        Assert.Equal(100m, saved.ReferenceQuantity);
        Assert.Equal(372.123456789m, saved.NutritionalValues.Kilocalories);
        Assert.Equal(13.567891234m, saved.NutritionalValues.ProteinGrams);
        Assert.Equal(58.765432198m, saved.NutritionalValues.CarbohydratesGrams);
        Assert.Equal(7.012345678m, saved.NutritionalValues.FatGrams);
    }

    [Fact]
    public async Task Post_WithExplicitZeroNutrition_PersistsZeros()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = JsonNode.Parse(ValidJson)!.AsObject();
        payload["brand"] = " \t";

        foreach (var property in NutrientProperties)
        {
            payload[property] = 0m;
        }

        using var response = await client.PostAsJsonAsync("/foods", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var context = new NutrivoDbContext(options);
        var saved = await context.Foods.AsNoTracking().SingleAsync();
        Assert.Null(saved.Brand);
        Assert.Equal(
            NutritionalValues.Create(0m, 0m, 0m, 0m).Value,
            saved.NutritionalValues
        );
    }

    public static TheoryData<string, string, string> InvalidDomainInputs => new()
    {
        { "name", "null", "Food.Name.Required" },
        { "name", "\"   \"", "Food.Name.Required" },
        { "name", JsonSerializer.Serialize(new string('a', 201)), "Food.Name.TooLong" },
        { "brand", JsonSerializer.Serialize(new string('b', 201)), "Food.Brand.TooLong" },
        { "referenceQuantity", "0", "Food.ReferenceQuantity.Invalid" },
        { "referenceQuantity", "-1", "Food.ReferenceQuantity.Invalid" },
        { "referenceUnit", "0", "Food.ReferenceUnit.Invalid" },
        { "referenceUnit", "3", "Food.ReferenceUnit.Invalid" },
        { "kilocalories", "-1", "NutritionalValues.Kilocalories.Negative" },
        { "proteinGrams", "-1", "NutritionalValues.ProteinGrams.Negative" },
        { "carbohydratesGrams", "-1", "NutritionalValues.CarbohydratesGrams.Negative" },
        { "fatGrams", "-1", "NutritionalValues.FatGrams.Negative" }
    };

    [Theory]
    [MemberData(nameof(InvalidDomainInputs))]
    public async Task Post_WithDomainFailure_ReturnsStableProblemAndSavesNothing(
        string property,
        string value,
        string expectedCode
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = JsonNode.Parse(ValidJson)!.AsObject();
        payload[property] = JsonNode.Parse(value);

        using var response = await client.PostAsJsonAsync("/foods", payload);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.False(
            string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString())
        );
        await AssertDatabaseEmptyAsync(options);
    }

    private static readonly string[] NutrientProperties =
        ["kilocalories", "proteinGrams", "carbohydratesGrams", "fatGrams"];

    public static IEnumerable<object[]> InvalidRequests()
    {
        foreach (var property in new[] { "name", "referenceQuantity", "referenceUnit" }
                     .Concat(
                         NutrientProperties
                     ))
        {
            var payload = JsonNode.Parse(ValidJson)!.AsObject();
            payload.Remove(property);
            yield return [payload.ToJsonString()];
        }

        foreach (var property in new[] { "referenceQuantity", "referenceUnit" }.Concat(
                     NutrientProperties
                 ))
        {
            var payload = JsonNode.Parse(ValidJson)!.AsObject();
            payload[property] = null;
            yield return [payload.ToJsonString()];
        }

        yield return ["{ invalid json"];
        yield return ["null"];
        yield return [""];
        yield return [ValidJson.Replace("372.123456789", "\"not a number\"")];
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Post_WithInvalidRequest_ReturnsProblemAndSavesNothing(string json)
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/foods", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        await AssertDatabaseEmptyAsync(options);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Post_WhenStorageFails_Returns500WithoutInternalDetails(
        string environment
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options, environment);
        await using var failingFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                    {
                        services.AddScoped<IFoodRepository, FailingFoodRepository>();
                    }
                );
            }
        );
        using var client = CreateClient(failingFactory);
        using var content = new StringContent(
            ValidJson,
            Encoding.UTF8,
            "application/json"
        );

        using var response = await client.PostAsync("/foods", content);

        var problem = await AssertProblemAsync(
            response,
            HttpStatusCode.InternalServerError
        );
        Assert.DoesNotContain("secret-storage-details", problem.GetRawText());
        Assert.DoesNotContain("InvalidOperationException", problem.GetRawText());
        Assert.False(problem.TryGetProperty("code", out _));
        await AssertDatabaseEmptyAsync(options);
    }

    [Fact]
    public async Task OpenApi_DescribesCreateFoodRequestAndResponses()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var operation = document
            .GetProperty("paths")
            .GetProperty("/foods")
            .GetProperty("post");
        Assert.Equal("CreateFood", operation.GetProperty("operationId").GetString());
        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("500", out _));
        Assert.Contains(
            "\"equivalentQuantity\": 58",
            operation.GetProperty("description").GetString()
        );
        var schemas = document.GetProperty("components").GetProperty("schemas");
        var requestSchema = schemas.GetProperty("CreateFoodRequest");
        Assert.Contains(
            requestSchema.GetProperty("required").EnumerateArray(),
            field => field.GetString() == "referenceQuantity"
        );
        Assert.True(
            requestSchema.GetProperty("properties").TryGetProperty("servingSizes", out _)
        );
        var servingRequired = schemas
            .GetProperty("CreateServingSizeRequest")
            .GetProperty("required")
            .EnumerateArray()
            .Select(field => field.GetString());
        Assert.Equal(new[] { "quantity", "unit", "equivalentQuantity" }, servingRequired);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PostAndSearch_WithServings_PreservesBasisAndReturnsRoundedNutrition(
        int unit
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        using var response = await client.PostAsJsonAsync(
            "/foods",
            new
            {
                name = "Egg",
                referenceQuantity = 50m,
                referenceUnit = unit,
                kilocalories = 150.005m,
                proteinGrams = 10.005m,
                carbohydratesGrams = 1.005m,
                fatGrams = 5.005m,
                servingSizes = new[]
                {
                    new
                    {
                        quantity = 2m, unit = " Stück ", label = " Mittel ",
                        equivalentQuantity = 116m
                    },
                    new
                    {
                        quantity = 1m, unit = "Stück", label = "Klein",
                        equivalentQuantity = 50m
                    }
                }
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();
        var result =
            (await client.GetFromJsonAsync<SearchFoodsResponse>("/foods?pageSize=1"))!;
        var food = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(id, food.Id);
        Assert.Equal(50m, food.ReferenceQuantity);
        Assert.Equal(150.005m, food.Kilocalories);
        Assert.Equal(10.005m, food.ProteinGrams);
        Assert.Equal(new[] { "Mittel", "Klein" }, food.ServingSizes.Select(s => s.Label));
        var pair = food.ServingSizes[0];
        Assert.Equal(2m, pair.Quantity);
        Assert.Equal("Stück", pair.Unit);
        Assert.Equal(116m, pair.EquivalentQuantity);
        Assert.Equal(348.01m, pair.Kilocalories);
        Assert.Equal(23.21m, pair.ProteinGrams);
        Assert.Equal(2.33m, pair.CarbohydratesGrams);
        Assert.Equal(11.61m, pair.FatGrams);
        Assert.Equal(150.01m, food.ServingSizes[1].Kilocalories);
        Assert.Equal(10.01m, food.ServingSizes[1].ProteinGrams);
        Assert.Equal(1.01m, food.ServingSizes[1].CarbohydratesGrams);
        Assert.Equal(5.01m, food.ServingSizes[1].FatGrams);

        // Serving labels do not change name/brand search semantics.
        var labelSearch =
            (await client.GetFromJsonAsync<SearchFoodsResponse>("/foods?search=Mittel"))!;
        Assert.Empty(labelSearch.Items);
        await using var context = new NutrivoDbContext(options);
        var stored = await context.Foods.SingleAsync();
        Assert.Equal(
            stored.ServingSizes.Select(s => s.Id.Value),
            food.ServingSizes.Select(s => s.Id)
        );
        Assert.Equal(150.005m, stored.NutritionalValues.Kilocalories);
    }

    public static TheoryData<string, string?> InvalidServings => new()
    {
        { "[null]", "Food.ServingSize.Required" },
        { "[{}]", null },
        { """[{"quantity":1,"unit":"piece"}]""", null },
        {
            """[{"quantity":0,"unit":"piece","equivalentQuantity":1}]""",
            "ServingSize.Quantity.Invalid"
        },
        {
            """[{"quantity":1,"unit":null,"equivalentQuantity":1}]""",
            "ServingSize.Unit.Required"
        },
        {
            """[{"quantity":1,"unit":" ","equivalentQuantity":1}]""",
            "ServingSize.Unit.Required"
        },
        {
            """[{"quantity":1,"unit":"piece","equivalentQuantity":-1}]""",
            "ServingSize.EquivalentQuantity.Invalid"
        },
        {
            """
            [
              {"quantity":1,"unit":"piece","equivalentQuantity":1},
              {"quantity":1,"unit":"piece","equivalentQuantity":0}
            ]
            """,
            "ServingSize.EquivalentQuantity.Invalid"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidServings))]
    public async Task Post_WithInvalidServing_RejectsEntireAggregate(
        string servings,
        string? code
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = JsonNode.Parse(ValidJson)!.AsObject();
        payload["servingSizes"] = JsonNode.Parse(servings);

        using var response = await client.PostAsJsonAsync("/foods", payload);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        if (code is not null)
        {
            Assert.Equal(code, problem.GetProperty("code").GetString());
        }

        await AssertDatabaseEmptyAsync(options);
    }

    [Fact]
    public async Task Post_WithCalculatedOverflow_ReturnsDomainError()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = CreateClient(factory);
        var payload = JsonNode.Parse(ValidJson)!.AsObject();
        payload["referenceQuantity"] = 1m;
        payload["kilocalories"] = decimal.MaxValue;
        payload["servingSizes"] = JsonNode.Parse(
            "[{\"quantity\":1,\"unit\":\"piece\",\"equivalentQuantity\":2}]"
        );

        using var response = await client.PostAsJsonAsync("/foods", payload);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(
            "Food.ServingSize.Nutrition.OutOfRange",
            problem.GetProperty("code").GetString()
        );
        await AssertDatabaseEmptyAsync(options);
    }

    private async Task<DbContextOptions<NutrivoDbContext>> CreateDatabaseAsync()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        return options;
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
        HttpStatusCode expectedStatus
    )
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        return problem;
    }

    private static async Task AssertDatabaseEmptyAsync(
        DbContextOptions<NutrivoDbContext> options
    )
    {
        await using var context = new NutrivoDbContext(options);
        Assert.False(await context.Foods.AnyAsync());
    }

    private sealed class FailingFoodRepository : IFoodRepository
    {
        public Task AddAsync(Food food, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("secret-storage-details");
        }
    }
}
