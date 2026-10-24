using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Nutrivo.Infrastructure.Persistence;

namespace Nutrivo.IntegrationTests.Foods;

internal sealed class FoodApiFactory(
    DbContextOptions<NutrivoDbContext> databaseOptions,
    string environment = "Development"
) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        using var context = new NutrivoDbContext(databaseOptions);
        builder.UseEnvironment(environment);
        builder.UseSetting(
            "ConnectionStrings:Nutrivo", context.Database.GetConnectionString()
        );
    }
}
