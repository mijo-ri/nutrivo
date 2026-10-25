using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nutrivo.Application.Abstractions;

namespace Nutrivo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return AddHandlers(services, typeof(DependencyInjection).Assembly.GetTypes());
    }

    internal static IServiceCollection AddHandlers(
        IServiceCollection services,
        IEnumerable<Type> types
    )
    {
        var registrations = types
            .Where(type => type.IsClass && !type.IsAbstract
                && !type.ContainsGenericParameters)
            .SelectMany(type => type.GetInterfaces()
                .Where(IsHandlerInterface)
                .Select(contract => new { Contract = contract, Implementation = type }))
            .GroupBy(registration => registration.Contract)
            .ToArray();

        foreach (var registration in registrations)
        {
            var implementations = registration.Select(item => item.Implementation)
                .Distinct()
                .ToArray();

            if (implementations.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple handlers implement '{registration.Key}': "
                    + string.Join(", ", implementations.Select(type => type.FullName))
                    + ". Register exactly one handler per command or query."
                );
            }
        }

        foreach (var registration in registrations)
        {
            var implementation = registration.First().Implementation;
            services.TryAddScoped(registration.Key, implementation);
        }

        return services;
    }

    private static bool IsHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<,>)
            || definition == typeof(IQueryHandler<,>);
    }
}
