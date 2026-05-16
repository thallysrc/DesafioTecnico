using FluentValidation;
using Inventory.Api.Dtos;

namespace Inventory.Api.Validators;

/// <summary>
/// Validates <see cref="CreateProductRequest"/> before the controller action runs.
/// Auto-discovered by <c>AddValidatorsFromAssemblyContaining&lt;Program&gt;()</c> in Program.cs.
/// On failure, FluentValidation throws <c>ValidationException</c> which the
/// <c>ExceptionHandlingMiddleware</c> maps to <c>400 VALIDATION_ERROR</c> with the
/// per-field <c>details.fields[]</c> shape consumed by the frontend.
///
/// PT-BR messages MUST match the Zod schema in
/// <c>frontend/src/features/products/schemas/createProductSchema.ts</c> byte-for-byte
/// (mirroring policy CONTEXT.md D-11; canonical table in 02-UI-SPEC.md §"Form field
/// validation messages").
/// </summary>
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Código é obrigatório")
            .MaximumLength(50).WithMessage("Código deve ter no máximo 50 caracteres");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(200).WithMessage("Descrição deve ter no máximo 200 caracteres");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo inválido. Valores aceitos: Eletrônico, Eletrodoméstico, Móvel");

        RuleFor(x => x.SupplierValue)
            .GreaterThanOrEqualTo(0).WithMessage("Valor do fornecedor não pode ser negativo");

        RuleFor(x => x.InitialStockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade inicial não pode ser negativa");
    }
}
