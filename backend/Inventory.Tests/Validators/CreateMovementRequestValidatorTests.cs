using FluentValidation.TestHelper;
using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Validators;

namespace Inventory.Tests.Validators;

/// <summary>
/// Locks the PT-BR validation messages byte-for-byte (mirrors 03-UI-SPEC §"Form field validation messages").
/// These strings are the source-of-truth for the frontend Zod schemas (frontend/src/features/stock/schemas.ts).
///
/// Note: the Inbound/Outbound value-field invariant (Inbound needs SupplierValue, Outbound needs
/// SaleValue) is enforced in <see cref="Inventory.Api.Services.StockMovementService"/> via
/// <see cref="Inventory.Api.Exceptions.InvalidMovementValuesException"/> (a 422 business rule),
/// NOT in this validator. Tests for that rule live in <c>StockMovementServiceTests</c>.
/// </summary>
public class CreateMovementRequestValidatorTests
{
    private readonly CreateMovementRequestValidator _validator = new();

    private static CreateMovementRequest Valid() => new(
        ProductId: Guid.Parse("7168c579-8259-4c0e-949b-998a7147c47f"),
        Type: MovementType.Inbound,
        Quantity: 5,
        SupplierValue: 100m,
        SaleValue: null
    );

    [Fact]
    public void ProductId_Should_Be_Required_With_Exact_PT_Message()
    {
        var req = Valid() with { ProductId = Guid.Empty };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
              .WithErrorMessage("Produto é obrigatório");
    }

    [Fact]
    public void Quantity_Should_Reject_Zero_With_Exact_PT_Message()
    {
        var req = Valid() with { Quantity = 0 };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
              .WithErrorMessage("Quantidade deve ser maior que zero");
    }

    [Fact]
    public void Quantity_Should_Reject_Negative_With_Exact_PT_Message()
    {
        var req = Valid() with { Quantity = -1 };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
              .WithErrorMessage("Quantidade deve ser maior que zero");
    }

    [Fact]
    public void SupplierValue_Should_Reject_Negative_When_Provided_With_Exact_PT_Message()
    {
        var req = Valid() with { SupplierValue = -0.01m };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.SupplierValue)
              .WithErrorMessage("Valor do fornecedor não pode ser negativo");
    }

    [Fact]
    public void SaleValue_Should_Reject_Negative_When_Provided_With_Exact_PT_Message()
    {
        var req = Valid() with
        {
            Type = MovementType.Outbound,
            SupplierValue = (decimal?)null,
            SaleValue = -0.01m
        };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.SaleValue)
              .WithErrorMessage("Valor de venda não pode ser negativo");
    }

    [Fact]
    public void Valid_Inbound_Request_Should_Pass_With_No_Errors()
    {
        var req = Valid();
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
