using FluentValidation;
using Inventory.Api.Dtos;

namespace Inventory.Api.Validators;

/// <summary>
/// Validates <see cref="CreateMovementRequest"/> before the controller action runs.
/// On failure, FluentValidation throws <c>ValidationException</c> which the
/// <c>ExceptionHandlingMiddleware</c> maps to <c>400 VALIDATION_ERROR</c>.
///
/// PT-BR messages MUST match the Zod schemas in
/// <c>frontend/src/features/stock/schemas.ts</c> byte-for-byte (mirroring policy from
/// CONTEXT.md D-11 carried over from Phase 2; canonical table in
/// 03-UI-SPEC §"Form field validation messages").
///
/// The Inbound/Outbound value-field invariant (Inbound needs SupplierValue, Outbound needs
/// SaleValue, mutual exclusion) is enforced in <c>StockMovementService</c> via
/// <see cref="Inventory.Api.Exceptions.InvalidMovementValuesException"/> (a 422 business rule
/// with a dynamic hint pointing at the correct field), NOT here.
/// </summary>
public class CreateMovementRequestValidator : AbstractValidator<CreateMovementRequest>
{
    public CreateMovementRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Produto é obrigatório");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1).WithMessage("Quantidade deve ser maior que zero");

        RuleFor(x => x.SupplierValue)
            .GreaterThanOrEqualTo(0).WithMessage("Valor do fornecedor não pode ser negativo")
            .When(x => x.SupplierValue.HasValue);

        RuleFor(x => x.SaleValue)
            .GreaterThanOrEqualTo(0).WithMessage("Valor de venda não pode ser negativo")
            .When(x => x.SaleValue.HasValue);
    }
}
