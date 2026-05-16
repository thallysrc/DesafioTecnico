using System.Data;

namespace Inventory.Api.Infra;

/// <summary>
/// Factory for short-lived <see cref="IDbConnection"/> instances backed by Npgsql.
/// Registered as a singleton in <c>Program.cs</c>; each <c>Create()</c> call returns a fresh
/// connection that the caller owns (and disposes via <c>using</c>).
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection Create();
}
