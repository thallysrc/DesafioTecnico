using Inventory.Api.Entities;

namespace Inventory.Api.Exceptions;

/// <summary>
/// Thrown when a movement payload supplies the wrong value field for its <c>type</c>:
/// Inbound MUST carry only <c>SupplierValue</c>; Outbound MUST carry only <c>SaleValue</c>.
/// Mapped to HTTP 422 with <c>errorCode: INVALID_MOVEMENT_VALUES</c> (MOVE-07).
/// </summary>
public class InvalidMovementValuesException : BusinessRuleException
{
    public InvalidMovementValuesException(MovementType type, string providedField, string expectedField)
        : base(
            errorCode: "INVALID_MOVEMENT_VALUES",
            message: $"Movimento do tipo '{type}' não aceita o campo '{providedField}'",
            hint: $"Movimentos do tipo '{type}' requerem o campo '{expectedField}', e não '{providedField}'.",
            details: new { type = type.ToString(), providedField, expectedField },
            retryable: false)
    { }
}
