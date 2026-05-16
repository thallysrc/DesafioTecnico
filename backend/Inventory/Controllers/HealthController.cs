using System.Reflection;
using Dapper;
using Inventory.Api.Dtos;
using Inventory.Api.Infra;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers;

/// <summary>Liveness / readiness probe for the StockEasy API.</summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IDbConnectionFactory factory, ILogger<HealthController> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>Returns service status and Postgres connectivity.</summary>
    /// <remarks>
    /// Pings the database via <c>SELECT 1</c>. Returns <c>200 OK</c> with <c>status: "ok"</c> on success,
    /// or <c>200 OK</c> with <c>status: "degraded"</c> and <c>db: "down"</c> if the ping fails — the API
    /// itself is up, only the dependency is impaired. The frontend health pill in <c>App.vue</c> consumes
    /// this on mount to render its three states.
    /// </remarks>
    [HttpGet]
    [SwaggerOperation(OperationId = "getHealth", Tags = new[] { "Health" })]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";

        var dbUp = false;
        try
        {
            using var conn = _factory.Create();
            // Dapper's ExecuteScalarAsync opens the connection lazily; no need to call conn.Open() first.
            var result = await conn.ExecuteScalarAsync<int>("SELECT 1");
            dbUp = result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health probe: Postgres SELECT 1 failed");
        }

        var response = new HealthResponse(
            Status: dbUp ? "ok" : "degraded",
            Db: dbUp ? "up" : "down",
            Version: version,
            Timestamp: DateTime.UtcNow.ToString("O")
        );

        return Ok(response);
    }
}
