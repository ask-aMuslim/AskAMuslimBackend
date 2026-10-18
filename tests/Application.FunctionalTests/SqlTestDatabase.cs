using System.Data.Common;
using AskAMuslimBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace AskAMuslimBackend.Application.FunctionalTests;

public class SqlTestDatabase : ITestDatabase
{
    private const string RequiredDatabaseSuffix = "_Tests";
    private readonly string _connectionString = null!;
    private NpgsqlConnection _connection = null!;
    private Respawner _respawner = null!;

    public SqlTestDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("AskAMuslimBackendDb");

        Guard.Against.Null(connectionString);

        // This fixture drops and recreates the database, so never let it touch a real one.
        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (databaseName == null || !databaseName.EndsWith(RequiredDatabaseSuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run functional tests against database '{databaseName}'. " +
                $"The fallback test database name must end with '{RequiredDatabaseSuffix}'.");
        }

        _connectionString = connectionString;
    }

    public async Task InitialiseAsync()
    {
        _connection = new NpgsqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        var context = new ApplicationDbContext(options);

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new Table[] { "__EFMigrationsHistory" }
        });
    }

    public DbConnection GetConnection()
    {
        return _connection;
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(_connection);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
