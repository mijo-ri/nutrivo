using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.LogFood;
using Nutrivo.Domain.Diaries;
using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.CreateFood;
using Nutrivo.Application.Foods.SearchFoods;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersApplicationHandlersByInterfaceAsScoped()
    {
        var services = new ServiceCollection();
        services.AddScoped<IFoodRepository, UnusedFoodRepository>();
        services.AddScoped<IDiaryEntryRepository, UnusedDiaryEntryRepository>();
        services.AddScoped<IFoodQueries, UnusedFoodQueries>();

        Assert.Same(services, services.AddApplication());
        services.AddApplication();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }
        );
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateFoodCommand, FoodId>>();
        var same = firstScope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateFoodCommand, FoodId>>();
        var second = secondScope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateFoodCommand, FoodId>>();

        var log = firstScope.ServiceProvider
            .GetRequiredService<ICommandHandler<LogFoodCommand, DiaryEntryId>>();
        Assert.IsType<LogFoodCommandHandler>(log);
        Assert.Same(
            log,
            firstScope.ServiceProvider
                .GetRequiredService<ICommandHandler<LogFoodCommand, DiaryEntryId>>()
        );
        Assert.NotSame(
            log,
            secondScope.ServiceProvider
                .GetRequiredService<ICommandHandler<LogFoodCommand, DiaryEntryId>>()
        );
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType
                == typeof(ICommandHandler<LogFoodCommand, DiaryEntryId>)
        );

        Assert.IsType<CreateFoodCommandHandler>(first);
        Assert.Same(first, same);
        Assert.NotSame(first, second);
        var query = firstScope.ServiceProvider
            .GetRequiredService<IQueryHandler<SearchFoodsQuery, SearchFoodsResult>>();
        Assert.IsType<SearchFoodsQueryHandler>(query);
        Assert.Same(
            query,
            firstScope.ServiceProvider
                .GetRequiredService<IQueryHandler<SearchFoodsQuery, SearchFoodsResult>>()
        );
        Assert.NotSame(
            query,
            secondScope.ServiceProvider
                .GetRequiredService<IQueryHandler<SearchFoodsQuery, SearchFoodsResult>>()
        );
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType
                == typeof(IQueryHandler<SearchFoodsQuery, SearchFoodsResult>)
        );
        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType
                == typeof(ICommandHandler<CreateFoodCommand, FoodId>)
        );
        Assert.DoesNotContain(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(CreateFoodCommand)
                || descriptor.ServiceType == typeof(CreateFoodCommandHandler)
                || descriptor.ImplementationType == typeof(CommandProcessor)
                || descriptor.ImplementationType == typeof(QueryProcessor)
        );
        Assert.Throws<InvalidOperationException>(provider
            .GetRequiredService<ICommandHandler<CreateFoodCommand, FoodId>>
        );
    }

    [Fact]
    public async Task AddHandlers_DiscoversCommandAndQueryContractsByInterface()
    {
        var services = new ServiceCollection();
        DependencyInjection.AddHandlers(
            services,
            [typeof(CommandProcessor), typeof(QueryProcessor)]
        );
        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }
        );
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var commandHandler = firstScope.ServiceProvider
            .GetRequiredService<ICommandHandler<TestCommand, string>>();
        var queryHandler = firstScope.ServiceProvider
            .GetRequiredService<IQueryHandler<TestQuery, string>>();

        var commandResult = await commandHandler.HandleAsync(
            new TestCommand(),
            CancellationToken.None
        );
        var queryResult = await queryHandler.HandleAsync(
            new TestQuery(),
            CancellationToken.None
        );

        Assert.Equal("command", commandResult.Value);
        Assert.Equal("query", queryResult.Value);
        Assert.Same(
            commandHandler,
            firstScope.ServiceProvider
                .GetRequiredService<ICommandHandler<TestCommand, string>>()
        );
        Assert.Same(
            queryHandler,
            firstScope.ServiceProvider
                .GetRequiredService<IQueryHandler<TestQuery, string>>()
        );
        Assert.NotSame(
            commandHandler,
            secondScope.ServiceProvider
                .GetRequiredService<ICommandHandler<TestCommand, string>>()
        );
        Assert.NotSame(
            queryHandler,
            secondScope.ServiceProvider
                .GetRequiredService<IQueryHandler<TestQuery, string>>()
        );
    }

    [Fact]
    public void AddHandlers_IgnoresInterfacesAbstractAndOpenGenericTypesAndOtherServices()
    {
        var services = new ServiceCollection();

        DependencyInjection.AddHandlers(
            services,
            [
                typeof(ICommandHandler<TestCommand, string>),
                typeof(AbstractProcessor),
                typeof(GenericProcessor<>),
                typeof(UnusedFoodRepository),
                typeof(TestCommand),
                typeof(TestQuery)
            ]
        );

        Assert.Empty(services);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddHandlers_WithDuplicateImplementations_ExplainsAmbiguousContract(
        bool query
    )
    {
        var services = new ServiceCollection();
        var first = query ? typeof(QueryProcessor) : typeof(CommandProcessor);
        var duplicate = query
            ? typeof(DuplicateQueryProcessor)
            : typeof(DuplicateCommandProcessor);
        var contract = query
            ? typeof(IQueryHandler<TestQuery, string>)
            : typeof(ICommandHandler<TestCommand, string>);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DependencyInjection.AddHandlers(services, [first, duplicate])
        );

        Assert.Contains(contract.ToString(), exception.Message);
        Assert.Contains(first.FullName!, exception.Message);
        Assert.Contains(duplicate.FullName!, exception.Message);
        Assert.Empty(services);
    }

    private sealed record TestCommand;

    private sealed record TestQuery;

    private class CommandProcessor : ICommandHandler<TestCommand, string>
    {
        public Task<Result<string>> HandleAsync(
            TestCommand command,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(Result<string>.Success("command"));
        }
    }

    private class QueryProcessor : IQueryHandler<TestQuery, string>
    {
        public Task<Result<string>> HandleAsync(
            TestQuery query,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(Result<string>.Success("query"));
        }
    }

    private sealed class DuplicateCommandProcessor : CommandProcessor;

    private sealed class DuplicateQueryProcessor : QueryProcessor;

    private abstract class AbstractProcessor : CommandProcessor;

    private sealed class GenericProcessor<T> : ICommandHandler<T, string>
    {
        public Task<Result<string>> HandleAsync(
            T command,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }
    }

    public sealed class UnusedDiaryEntryRepository : IDiaryEntryRepository
    {
        public Task AddAsync(DiaryEntry entry, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class UnusedFoodRepository : IFoodRepository
    {
        public Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Food food, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class UnusedFoodQueries : IFoodQueries
    {
        public Task<SearchFoodsResult> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }
    }
}
