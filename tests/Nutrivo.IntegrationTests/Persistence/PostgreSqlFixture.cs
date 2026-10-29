using Microsoft.EntityFrameworkCore;
using Npgsql;
using Nutrivo.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Nutrivo.IntegrationTests.Persistence;

[CollectionDefinition("PostgreSQL")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18.6").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<DbContextOptions<NutrivoDbContext>> CreateDatabaseAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Each test owns a database; rolling back migrations cannot affect other tests.
        var databaseName = $"nutrivo_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(
            _container.GetConnectionString()
        );
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);

        var connectionString = new NpgsqlConnectionStringBuilder(
            _container.GetConnectionString()
        )
        {
            Database = databaseName,
            // Each test has a distinct database; idle per-database pools would
            // otherwise exhaust the container's connection limit as the suite grows.
            Pooling = false
        };

        return new DbContextOptionsBuilder<NutrivoDbContext>()
            .UseNpgsql(connectionString.ConnectionString)
            .Options;
    }
}
