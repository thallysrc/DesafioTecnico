using FluentValidation.TestHelper;
using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Validators;

namespace Inventory.Tests.Validators;

/// <summary>
/// Locks the PT-BR validation messages byte-for-byte (mirrors 02-UI-SPEC §"Form field validation messages").
/// These strings are the source-of-truth for the frontend Zod schema (Plan 02-05).
/// </summary>
public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    private static CreateProductRequest Valid() => new(
        Code: "P001",
        Description: "Notebook",
        Type: ProductType.Electronic,
        SupplierValue: 100m,
        InitialStockQuantity: 5
    );

    [Fact]
    public void Code_Should_Be_Required_With_Exact_PT_Message()
    {
        var req = Valid() with { Code = string.Empty };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Code)
              .WithErrorMessage("Código é obrigatório");
    }

    [Fact]
    public void Code_Should_Reject_Length_Above_50_With_Exact_PT_Message()
    {
        var req = Valid() with { Code = new string('A', 51) };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Code)
              .WithErrorMessage("Código deve ter no máximo 50 caracteres");
    }

    [Fact]
    public void Description_Should_Be_Required_With_Exact_PT_Message()
    {
        var req = Valid() with { Description = string.Empty };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Descrição é obrigatória");
    }

    [Fact]
    public void Description_Should_Reject_Length_Above_200_With_Exact_PT_Message()
    {
        var req = Valid() with { Description = new string('A', 201) };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Descrição deve ter no máximo 200 caracteres");
    }

    [Fact]
    public void Type_Should_Reject_Out_Of_Range_With_Exact_PT_Message()
    {
        var req = Valid() with { Type = (ProductType)99 };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Type)
              .WithErrorMessage("Tipo inválido. Valores aceitos: Eletrônico, Eletrodoméstico, Móvel");
    }

    [Fact]
    public void SupplierValue_Should_Reject_Negative_With_Exact_PT_Message()
    {
        var req = Valid() with { SupplierValue = -0.01m };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.SupplierValue)
              .WithErrorMessage("Valor do fornecedor não pode ser negativo");
    }

    [Fact]
    public void InitialStockQuantity_Should_Reject_Negative_With_Exact_PT_Message()
    {
        var req = Valid() with { InitialStockQuantity = -1 };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.InitialStockQuantity)
              .WithErrorMessage("Quantidade inicial não pode ser negativa");
    }

    [Fact]
    public void Valid_Request_Should_Pass_With_No_Errors()
    {
        var req = Valid();
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
