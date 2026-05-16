namespace Inventory.Api.Exceptions;

/// <summary>
/// Thrown when <c>GET /api/stock-movements/{id}</c> targets an id that doesn't exist.
/// Mapped to HTTP 404 with <c>errorCode: MOVEMENT_NOT_FOUND</c> (MOVE-10).
/// </summary>
public class MovementNotFoundException : NotFoundException
{
    public MovementNotFoundException(Guid movementId)
        : base(
            errorCode: "MOVEMENT_NOT_FOUND",
            message: "Movimento não encontrado",
            hint: "Movimento não encontrado. Atualize a página e tente novamente.",
            details: new { movementId })
    { }
}
