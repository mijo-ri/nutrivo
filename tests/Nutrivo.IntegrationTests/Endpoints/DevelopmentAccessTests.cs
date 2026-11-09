using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Nutrivo.IntegrationTests.Endpoints;

public sealed class DevelopmentAccessTests
{
    [Theory]
    [InlineData("http://localhost:8081", "POST")]
    [InlineData("http://localhost:8081", "PUT")]
    [InlineData("http://127.0.0.1:8081", "POST")]
    [InlineData("http://127.0.0.1:8081", "PUT")]
    public async Task Development_AllowsConfiguredWebOriginsAndWritePreflight(
        string origin,
        string method
    )
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            method == "PUT" ? $"/foods/{Guid.NewGuid()}" : "/foods"
        );
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            origin,
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin"))
        );
        Assert.Contains(
            method,
            response.Headers.GetValues("Access-Control-Allow-Methods").Single()
        );
        Assert.Contains(
            "content-type",
            response.Headers.GetValues("Access-Control-Allow-Headers").Single(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task Development_AllowsGetWithoutRedirectAndRejectsUnknownOrigin()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );
        foreach (var origin in new[]
                 {
                     "http://localhost:8081", "https://untrusted.example"
                 })
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/foods/not-a-guid"
            );
            request.Headers.Add("Origin", origin);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                origin == "http://localhost:8081",
                response.Headers.Contains("Access-Control-Allow-Origin")
            );
        }
    }

    [Fact]
    public async Task Development_UsesConfiguredAdditionalOrigin()
    {
        await using var factory = CreateFactory("Development")
            .WithWebHostBuilder(builder =>
                builder.UseSetting("Cors:AllowedOrigins:0", "http://192.168.1.10:8082")
            );
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/foods/not-a-guid");
        request.Headers.Add("Origin", "http://192.168.1.10:8082");
        using var response = await client.SendAsync(request);
        Assert.Equal(
            "http://192.168.1.10:8082",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single()
        );
    }

    [Fact]
    public async Task Production_PreservesHttpsRedirectAndDoesNotEnableDevelopmentCors()
    {
        await using var factory = CreateFactory("Production")
            .WithWebHostBuilder(builder =>
                builder.UseSetting("https_port", "7179")
            );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );
        using var request = new HttpRequestMessage(HttpMethod.Get, "/foods/not-a-guid");
        request.Headers.Add("Origin", "http://localhost:8081");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal("https", response.Headers.Location?.Scheme);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting(
                    "ConnectionStrings:Nutrivo",
                    "Host=localhost;Database=unused"
                );
            }
        );
}
