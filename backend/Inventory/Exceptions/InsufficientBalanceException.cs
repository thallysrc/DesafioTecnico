namespace Inventory.Api.Exceptions;

/// <summary>
/// Thrown when an Outbound movement requests more units than the product currently has in stock.
/// Mapped to HTTP 422 with <c>errorCode: INSUFFICIENT_BALANCE</c> (MOVE-04).
/// </summary>
public class InsufficientBalanceException : BusinessRuleException
{
    public InsufficientBalanceException(Guid productId, string productCode, int requested, int available)
        : base(
            errorCode: "INSUFFICIENT_BALANCE",
            message: $"Saldo insuficiente: solicitado {requested} unidades, disponível {available}",
            hint: $"Reduza a quantidade para no máximo {available} ou registre uma entrada antes.",
            details: new
            {
                productId,
                productCode,
                requested,
                available,
                deficit = requested - available
            },
            retryable: false)
    { }
}
