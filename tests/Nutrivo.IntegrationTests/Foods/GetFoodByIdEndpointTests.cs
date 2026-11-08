using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Api.Endpoints.Foods.SearchFoods;
using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.SearchFoods;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Foods;

[Collection("PostgreSQL")]
public sealed class GetFoodByIdEndpointTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(ReferenceUnit.Gram, null)]
    [InlineData(ReferenceUnit.Milliliter, "Farm")]
    public async Task Get_ReturnsPersistedFoodAndOrderedServingsWithSearchRounding(
        ReferenceUnit unit,
        string? brand
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        using var created = await client.PostAsJsonAsync(
            "/foods",
            new
            {
                name = "  Milk  ",
                brand,
                referenceQuantity = 100m,
                referenceUnit = unit,
                kilocalories = 10.125m,
                proteinGrams = 2.123456m,
                carbohydratesGrams = 3.125m,
                fatGrams = 1.125m,
                servingSizes = new[]
                {
                    new
                    {
                        quantity = 2m,
                        unit = "Becher",
                        label = "Gross",
                        equivalentQuantity = 200m
                    },
                    new
                    {
                        quantity = 1m,
                        unit = "Löffel",
                        label = "Klein",
                        equivalentQuantity = 100m
                    }
                }
            }
        );
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        using var response = await client.GetAsync($"/foods/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var food = (await response.Content.ReadFromJsonAsync<SearchFoodResponse>())!;
        Assert.Equal(id, food.Id);
        Assert.Equal("Milk", food.Name);
        Assert.Equal(brand, food.Brand);
        Assert.Equal(unit, food.ReferenceUnit);
        Assert.Equal(100m, food.ReferenceQuantity);
        Assert.Equal(10.125m, food.Kilocalories);
        Assert.Equal(2.123456m, food.ProteinGrams);
        Assert.Equal(3.125m, food.CarbohydratesGrams);
        Assert.Equal(1.125m, food.FatGrams);
        Assert.Equal(
            new[]
            {
                "Gross", "Klein"
            },
            food.ServingSizes.Select(size => size.Label)
        );
        Assert.All(food.ServingSizes, size => Assert.NotEqual(Guid.Empty, size.Id));
        Assert.Equal(2, food.ServingSizes.Select(size => size.Id).Distinct().Count());
        Assert.Equal(20.25m, food.ServingSizes[0].Kilocalories);
        Assert.Equal(10.13m, food.ServingSizes[1].Kilocalories);
        Assert.Equal(2.12m, food.ServingSizes[1].ProteinGrams);
        Assert.Equal(3.13m, food.ServingSizes[1].CarbohydratesGrams);
        Assert.Equal(1.13m, food.ServingSizes[1].FatGrams);
        var search = (await client.GetFromJsonAsync<SearchFoodsResponse>("/foods"))!;
        Assert.Equal(
            JsonSerializer.Serialize(Assert.Single(search.Items)),
            JsonSerializer.Serialize(food)
        );
        var again = (await client.GetFromJsonAsync<SearchFoodResponse>($"/foods/{id}"))!;
        Assert.Equal(
            food.ServingSizes.Select(size => size.Id),
            again.ServingSizes.Select(size => size.Id)
        );
    }

    [Fact]
    public async Task Get_WithNoServings_ReturnsEmptyServingsAndDoesNotTrack()
    {
        var options = await CreateDatabaseAsync();
        var food = Food.Create(
                "Oats",
                null,
                75m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(123m, 4m, 5m, 6m).Value
            )
            .Value;
        await using (var context = new NutrivoDbContext(options))
        {
            context.Foods.Add(food);
            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var response =
            (await client.GetFromJsonAsync<SearchFoodResponse>($"/foods/{food.Id.Value}"))
            !;
        Assert.Empty(response.ServingSizes);
        Assert.Null(response.Brand);
        Assert.Equal(75m, response.ReferenceQuantity);
        using var scope = factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IFoodQueries>();
        Assert.NotNull(await queries.GetByIdAsync(food.Id, CancellationToken.None));
        Assert.Empty(
            scope
                .ServiceProvider.GetRequiredService<NutrivoDbContext>()
                .ChangeTracker.Entries()
        );
        Assert.Null(await queries.GetByIdAsync(FoodId.New(), CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            queries.GetByIdAsync(food.Id, cancellation.Token)
        );
    }

    [Theory]
    [InlineData("not-a-guid", 400, null)]
    [InlineData("00000000-0000-0000-0000-000000000000", 400, "Food.Id.Empty")]
    [InlineData("11111111-1111-1111-1111-111111111111", 404, "GetFoodById.NotFound")]
    public async Task Get_WithInvalidOrMissingId_ReturnsProblem(
        string id,
        int status,
        string? code
    )
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/foods/{id}");
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (code is not null)
            Assert.Equal(code, problem.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Get_WhenStorageFails_ReturnsGeneric500(string environment)
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options, environment);
        await using var failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddScoped<IFoodQueries, FailingFoodQueries>()
            )
        );
        using var client = failing.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            }
        );
        using var response = await client.GetAsync($"/foods/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-storage-details", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    [Fact]
    public async Task OpenApi_DescribesDetailEndpoint()
    {
        var options = await CreateDatabaseAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var operation = document
            .GetProperty("paths")
            .GetProperty("/foods/{id}")
            .GetProperty("get");
        Assert.Equal("GetFoodById", operation.GetProperty("operationId").GetString());
        var parameter =
            Assert.Single(operation.GetProperty("parameters").EnumerateArray());
        Assert.Equal("path", parameter.GetProperty("in").GetString());
        Assert.True(parameter.GetProperty("required").GetBoolean());
        foreach (var status in new[]
                 {
                     "200", "400", "404", "500"
                 })
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
    }

    private async Task<DbContextOptions<NutrivoDbContext>> CreateDatabaseAsync()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        return options;
    }

    private sealed class FailingFoodQueries : IFoodQueries
    {
        public Task<FoodReadModel?> GetByIdAsync(
            FoodId id,
            CancellationToken cancellationToken
        )
            => throw new InvalidOperationException("secret-storage-details");

        public Task<SearchFoodsResult> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
