using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Api.Endpoints;
using Nutrivo.Api.Endpoints.Foods.CreateFood;
using Nutrivo.Api.Endpoints.Foods.SearchFoods;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Foods;

namespace Nutrivo.IntegrationTests.Endpoints;

public sealed class EndpointRegistrationTests
{
    [Fact]
    public void AddEndpoints_RegistersOnlyApiEndpointsOnceAsSingletons()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddEndpoints());
        services.AddEndpoints();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }
        );
        var endpoints = provider.GetServices<IEndpoint>().ToArray();
        var sameEndpoints = provider.GetServices<IEndpoint>().ToArray();

        Assert.Equal(2, endpoints.Length);
        Assert.Single(endpoints.OfType<CreateFoodEndpoint>());
        Assert.Single(endpoints.OfType<SearchFoodsEndpoint>());
        Assert.All(
            services,
            descriptor =>
                Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime)
        );
        for (var index = 0; index < endpoints.Length; index++)
        {
            Assert.Same(endpoints[index], sameEndpoints[index]);
        }
    }

    [Fact]
    public async Task AutomaticallyMappedFoodEndpoint_ResolvesHandlerAndReturnsCreated()
    {
        var repository = new RecordingFoodRepository();
        await using var factory = CreateFactory(repository);
        using var client = CreateClient(factory);

        using var response = await client.PostAsJsonAsync(
            "/foods",
            new
            {
                name = "Oats",
                referenceQuantity = 100m,
                referenceUnit = 1,
                kilocalories = 372m,
                proteinGrams = 13.5m,
                carbohydratesGrams = 58.7m,
                fatGrams = 7m
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var saved = Assert.Single(repository.Foods);
        Assert.Equal(saved.Id.Value, body.GetProperty("id").GetGuid());
        Assert.Equal("Oats", saved.Name);
    }

    [Fact]
    public async Task AutomaticallyMappedEndpoints_PreserveOpenApiMetadata()
    {
        await using var factory = CreateFactory(new RecordingFoodRepository());
        using var client = CreateClient(factory);

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var paths = document.GetProperty("paths");
        var food = paths.GetProperty("/foods").GetProperty("post");
        Assert.Equal("CreateFood", food.GetProperty("operationId").GetString());
        Assert.Equal(
            "Creates a food with a nutrition basis and optional serving sizes.",
            food.GetProperty("summary").GetString()
        );
        var responses = food.GetProperty("responses");
        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("500", out _));
    }

    private static WebApplicationFactory<Program> CreateFactory(
        RecordingFoodRepository repository
    )
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting(
                    "ConnectionStrings:Nutrivo",
                    "Host=localhost;Database=unused"
                );
                builder.ConfigureServices(services =>
                    services.AddScoped<IFoodRepository>(_ => repository)
                );
            }
        );
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

    private sealed class RecordingFoodRepository : IFoodRepository
    {
        public List<Food> Foods { get; } = [];

        public Task AddAsync(Food food, CancellationToken cancellationToken)
        {
            Foods.Add(food);
            return Task.CompletedTask;
        }
    }
}
