namespace Inventory.Api.Exceptions;

/// <summary>
/// Thrown when <c>POST /api/stock-movements</c> is invoked without an <c>Idempotency-Key</c>
/// header. Mapped to HTTP 400 with <c>errorCode: MISSING_IDEMPOTENCY_KEY</c> (MOVE-02).
///
/// Category is <c>VALIDATION</c> rather than <c>BUSINESS_RULE</c> because the failure is a
/// request-shape problem at the protocol layer — equivalent to a missing required field —
/// and the auto-mapping in <c>ExceptionHandlingMiddleware</c> routes <c>VALIDATION</c> to 400.
/// </summary>
public class MissingIdempotencyKeyException : DomainException
{
    public MissingIdempotencyKeyException()
        : base(
            message: "Header 'Idempotency-Key' é obrigatório para registrar movimentos",
            errorCode: "MISSING_IDEMPOTENCY_KEY",
            category: "VALIDATION",
            hint: "Gere um UUID v4 no cliente (crypto.randomUUID()) e envie no header 'Idempotency-Key'.",
            retryable: true,
            details: null)
    { }
}
