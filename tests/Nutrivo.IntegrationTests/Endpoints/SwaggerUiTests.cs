using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Nutrivo.IntegrationTests.Endpoints;

public sealed class SwaggerUiTests
{
    [Fact]
    public async Task Development_ServesSwaggerUiAndExistingOpenApiDocument()
    {
        await using var factory = CreateFactory("Development");
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("swagger-ui", html);

        var initializer = await client.GetStringAsync("/swagger/index.js");
        Assert.Contains("/openapi/v1.json", initializer);

        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var operation = document
            .GetProperty("paths")
            .GetProperty("/foods")
            .GetProperty("post");
        Assert.Equal("CreateFood", operation.GetProperty("operationId").GetString());
    }

    [Theory]
    [InlineData("/swagger/swagger-ui.css", "text/css")]
    [InlineData("/swagger/swagger-ui-bundle.js", "text/javascript")]
    [InlineData("/swagger/swagger-ui-standalone-preset.js", "text/javascript")]
    public async Task Development_ServesSwaggerUiAssets(string path, string mediaType)
    {
        await using var factory = CreateFactory("Development");
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task OtherEnvironments_DoNotServeSwaggerUiOrOpenApi(string environment)
    {
        await using var factory = CreateFactory(environment);
        using var client = CreateClient(factory);
        string[] paths =
        [
            "/swagger",
            "/swagger/index.html",
            "/swagger/index.js",
            "/swagger/swagger-ui.css",
            "/swagger/swagger-ui-bundle.js",
            "/swagger/swagger-ui-standalone-preset.js",
            "/openapi/v1.json"
        ];

        foreach (var path in paths)
        {
            using var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting(
                    "ConnectionStrings:Nutrivo",
                    "Host=localhost;Database=unused"
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
}
