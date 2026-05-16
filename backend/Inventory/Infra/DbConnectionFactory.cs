using System.Data;
using Npgsql;

namespace Inventory.Api.Infra;

/// <summary>
/// Default <see cref="IDbConnectionFactory"/> implementation. Reads the connection string from
/// the <c>ConnectionStrings:Postgres</c> config key (per INFRA-06), overridable in docker-compose
/// via the <c>ConnectionStrings__Postgres</c> environment variable.
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres is not configured. Set it in appsettings.json or via the " +
                "ConnectionStrings__Postgres environment variable.");
    }

    public IDbConnection Create() => new NpgsqlConnection(_connectionString);
}
