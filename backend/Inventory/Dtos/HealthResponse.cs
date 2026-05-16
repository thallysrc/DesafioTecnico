namespace Inventory.Api.Dtos;

/// <summary>
/// Response shape for <c>GET /api/health</c>. The frontend health pill in App.vue reads
/// <c>status</c> + <c>db</c> to render its three states (checking / connected / offline).
/// </summary>
/// <param name="Status">Overall service status: <c>"ok"</c> when DB ping succeeded, <c>"degraded"</c> otherwise.</param>
/// <param name="Db">DB connectivity: <c>"up"</c> or <c>"down"</c>.</param>
/// <param name="Version">Assembly informational version (e.g. <c>"1.0.0"</c>).</param>
/// <param name="Timestamp">ISO 8601 UTC timestamp of the health probe.</param>
public record HealthResponse(string Status, string Db, string Version, string Timestamp);
