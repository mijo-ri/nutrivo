using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Infrastructure.Persistence;

namespace Nutrivo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("Nutrivo");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Nutrivo' is required. Configure "
                + "'ConnectionStrings:Nutrivo' with .NET User Secrets or the "
                + "'ConnectionStrings__Nutrivo' environment variable."
            );
        }

        services.AddDbContext<NutrivoDbContext>(
            options => options.UseNpgsql(connectionString)
        );

        return services;
    }
}
