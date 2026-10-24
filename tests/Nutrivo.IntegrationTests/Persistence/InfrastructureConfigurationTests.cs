using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Infrastructure;
using Nutrivo.Infrastructure.Persistence;

namespace Nutrivo.IntegrationTests.Persistence;

public sealed class InfrastructureConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInfrastructure_WithoutConnectionString_ExplainsHowToConfigureIt(
        string? connectionString
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Nutrivo"] = connectionString
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(configuration)
        );

        Assert.Contains("ConnectionStrings:Nutrivo", exception.Message);
        Assert.Contains("ConnectionStrings__Nutrivo", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_RegistersScopedPostgreSqlContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Nutrivo"] = "Host=localhost;Database=nutrivo"
            })
            .Build();
        using var services = new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var firstScope = services.CreateScope();
        using var secondScope = services.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<NutrivoDbContext>();
        var same = firstScope.ServiceProvider.GetRequiredService<NutrivoDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<NutrivoDbContext>();

        Assert.Same(first, same);
        Assert.NotSame(first, second);
        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            first.Database.ProviderName
        );
    }
}
