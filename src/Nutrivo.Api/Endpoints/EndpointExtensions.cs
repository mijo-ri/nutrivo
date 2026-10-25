using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nutrivo.Api.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointTypes = typeof(IEndpoint)
            .Assembly.GetTypes()
            .Where(type => type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && typeof(IEndpoint).IsAssignableFrom(type)
            )
            .OrderBy(type => type.FullName);

        foreach (var type in endpointTypes)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton(typeof(IEndpoint), type)
            );
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var endpoint in endpoints.ServiceProvider.GetServices<IEndpoint>())
        {
            endpoint.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
