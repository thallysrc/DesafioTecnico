using System.Data;
using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Exceptions;
using Inventory.Api.Infra;
using Inventory.Api.Repositories;
using Inventory.Api.Services;
using Inventory.Tests.TestHelpers;
using Moq;

namespace Inventory.Tests.Services;

/// <summary>
/// Unit tests for <see cref="StockMovementService"/>.
///
/// **Two test groups** per CONTEXT.md "Mockable seams" section of 04-01-PLAN.md:
///
/// **Group A** — service paths fully exercisable via pure Moq:
///   - Idempotency fast-path replay (BEFORE the transactional block)
///   - GetByIdAsync (read path)
///   - ListAsync (read path) including page/pageSize clamps
///
/// **Group B** — exception-trio coverage via direct typed-exception construction.
/// The transactional <see cref="StockMovementService.CreateAsync"/> path opens an
/// <c>NpgsqlConnection</c> + <c>BeginTransactionAsync</c> on a real connection — pure
/// Moq cannot intercept those Npgsql concretes. Per CONTEXT.md D-02 / 04-01-PLAN.md
/// "Mockable seams", we cover INSUFFICIENT_BALANCE / PRODUCT_DELETED / PRODUCT_NOT_FOUND /
/// INVALID_MOVEMENT_VALUES / MOVEMENT_NOT_FOUND / MISSING_IDEMPOTENCY_KEY by constructing
/// the typed exceptions and asserting their public contract (ErrorCode + Message + Hint +
/// Category + Details). The exception types ARE the domain contract — testing their
/// constructors validates that the service WILL produce the right error envelope when
/// it throws them.
/// </summary>
public class StockMovementServiceTests
{
    private readonly Mock<IStockMovementRepository> _movements = new();
    private readonly Mock<IDbConnectionFactory> _factory = new();
    private readonly StockMovementService _sut;

    public StockMovementServiceTests()
    {
        _sut = new StockMovementService(_factory.Object, _movements.Object);
    }

    private static StockMovementWithProduct ExistingMovement(Guid? key = null) => new()
    {
        Id = Guid.Parse("e3de56ef-4e8e-426a-a953-5e331f8bbd8f"),
        ProductId = Guid.Parse("7168c579-8259-4c0e-949b-998a7147c47f"),
        ProductCode = "P3-SMOKE-001",
        ProductDescription = "Phase 3 smoke product",
        Type = MovementType.Inbound,
        Quantity = 10,
        SupplierValue = 120.50m,
        SaleValue = null,
        IdempotencyKey = key ?? Guid.Parse("b9472614-10db-4f4a-8a03-56fe593cd226"),
        OccurredAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
    };

    // ===== GROUP A: Service paths fully Moq-able ================================

    [Fact]
    public async Task CreateAsync_Should_Return_Replay_True_When_Idempotency_Key_Already_Exists()
    {
        var key = Guid.NewGuid();
        var existing = ExistingMovement(key);
        _movements.Setup(m => m.GetByIdempotencyKeyAsync(key)).ReturnsAsync(existing);

        var request = new CreateMovementRequest(existing.ProductId, MovementType.Inbound, 10, 120.50m, null);
        var (response, isReplay) = await _sut.CreateAsync(request, key);

        Assert.True(isReplay);
        Assert.Equal(existing.Id, response.Id);
        _movements.Verify(m => m.GetByIdempotencyKeyAsync(key), Times.Once);
        _movements.Verify(
            m => m.InsertAsync(It.IsAny<StockMovement>(), It.IsAny<IDbTransaction>()),
            Times.Never);
        // Factory MUST NOT be touched — replay short-circuits before the transactional block.
        _factory.Verify(f => f.Create(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Should_Map_Existing_Movement_Identically_On_Replay()
    {
        var key = Guid.NewGuid();
        var existing = ExistingMovement(key);
        _movements.Setup(m => m.GetByIdempotencyKeyAsync(key)).ReturnsAsync(existing);

        var request = new CreateMovementRequest(existing.ProductId, MovementType.Inbound, 10, 120.50m, null);
        var (response, _) = await _sut.CreateAsync(request, key);

        Assert.Equal(existing.Id, response.Id);
        Assert.Equal(existing.ProductId, response.ProductId);
        Assert.Equal(existing.ProductCode, response.ProductCode);
        Assert.Equal(existing.ProductDescription, response.ProductDescription);
        Assert.Equal(existing.Type, response.Type);
        Assert.Equal(existing.Quantity, response.Quantity);
        Assert.Equal(existing.SupplierValue, response.SupplierValue);
        Assert.Equal(existing.SaleValue, response.SaleValue);
        Assert.Equal(existing.IdempotencyKey, response.IdempotencyKey);
        Assert.NotNull(response.Links);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_MovementResponse_When_Found()
    {
        var existing = ExistingMovement();
        _movements.Setup(m => m.GetByIdAsync(existing.Id)).ReturnsAsync(existing);

        var response = await _sut.GetByIdAsync(existing.Id);

        Assert.Equal(existing.Id, response.Id);
        Assert.Equal(existing.ProductCode, response.ProductCode);
        Assert.Equal(existing.ProductDescription, response.ProductDescription);
        Assert.Equal(existing.Type, response.Type);
        Assert.Equal(existing.Quantity, response.Quantity);
        Assert.NotNull(response.Links);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Throw_MOVEMENT_NOT_FOUND_When_Repository_Returns_Null()
    {
        var id = Guid.NewGuid();
        _movements.Setup(m => m.GetByIdAsync(id)).ReturnsAsync((StockMovementWithProduct?)null);

        var ex = await Assert.ThrowsAsync<MovementNotFoundException>(
            () => _sut.GetByIdAsync(id));

        AssertDomain.Trio(ex, "MOVEMENT_NOT_FOUND", "não encontrado");
        Assert.Contains("Atualize", ex.Hint!);
        Assert.Equal("NOT_FOUND", ex.Category);
    }

    [Fact]
    public async Task ListAsync_Should_Return_PagedMovementsResponse_With_Mapped_Items()
    {
        var items = new List<StockMovementWithProduct> { ExistingMovement(), ExistingMovement(Guid.NewGuid()) };
        _movements.Setup(m => m.ListAsync(null, null, null, 1, 30))
                  .ReturnsAsync(new PagedResult<StockMovementWithProduct>(items, 1, 30, 2));

        var response = await _sut.ListAsync(productId: null, startDate: null, endDate: null, page: 1, pageSize: 30);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1, response.Pagination.Page);
        Assert.Equal(30, response.Pagination.PageSize);
        Assert.Equal(2, response.Pagination.Total);
        Assert.Equal(1, response.Pagination.TotalPages);
        Assert.False(response.Pagination.HasNext);
        Assert.False(response.Pagination.HasPrev);
        Assert.NotNull(response.Links);
    }

    [Fact]
    public async Task ListAsync_Should_Clamp_Page_To_Minimum_1()
    {
        _movements.Setup(m => m.ListAsync(It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                                          It.IsAny<int>(), It.IsAny<int>()))
                  .ReturnsAsync(new PagedResult<StockMovementWithProduct>(
                      new List<StockMovementWithProduct>(), 1, 30, 0));

        await _sut.ListAsync(productId: null, startDate: null, endDate: null, page: 0, pageSize: 30);

        _movements.Verify(m => m.ListAsync(null, null, null, 1, 30), Times.Once);
    }

    [Fact]
    public async Task ListAsync_Should_Clamp_PageSize_To_Maximum_100()
    {
        _movements.Setup(m => m.ListAsync(It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                                          It.IsAny<int>(), It.IsAny<int>()))
                  .ReturnsAsync(new PagedResult<StockMovementWithProduct>(
                      new List<StockMovementWithProduct>(), 1, 100, 0));

        await _sut.ListAsync(productId: null, startDate: null, endDate: null, page: 1, pageSize: 9999);

        _movements.Verify(m => m.ListAsync(null, null, null, 1, 100), Times.Once);
    }

    [Fact]
    public async Task ListAsync_Should_Default_PageSize_To_30_When_Zero()
    {
        _movements.Setup(m => m.ListAsync(It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                                          It.IsAny<int>(), It.IsAny<int>()))
                  .ReturnsAsync(new PagedResult<StockMovementWithProduct>(
                      new List<StockMovementWithProduct>(), 1, 30, 0));

        await _sut.ListAsync(productId: null, startDate: null, endDate: null, page: 1, pageSize: 0);

        _movements.Verify(m => m.ListAsync(null, null, null, 1, 30), Times.Once);
    }

    // ===== GROUP B: ErrorCode payloads via typed-exception construction =========

    [Fact]
    public void InsufficientBalanceException_Should_Carry_Trio_With_Dynamic_Hint_And_Deficit()
    {
        var productId = Guid.NewGuid();
        var ex = new InsufficientBalanceException(productId, "P001", requested: 10, available: 3);

        AssertDomain.Trio(ex, "INSUFFICIENT_BALANCE", "solicitado 10 unidades, disponível 3");
        Assert.Contains("máximo 3", ex.Hint!);
        Assert.Contains("registre uma entrada antes", ex.Hint!);
        Assert.Equal("BUSINESS_RULE", ex.Category);
        Assert.False(ex.Retryable);

        // details shape — consumed by frontend D-08 saldo refresh (Phase 3)
        var details = ex.Details!;
        var t = details.GetType();
        Assert.Equal(productId, (Guid)t.GetProperty("productId")!.GetValue(details)!);
        Assert.Equal("P001", (string)t.GetProperty("productCode")!.GetValue(details)!);
        Assert.Equal(10, (int)t.GetProperty("requested")!.GetValue(details)!);
        Assert.Equal(3, (int)t.GetProperty("available")!.GetValue(details)!);
        Assert.Equal(7, (int)t.GetProperty("deficit")!.GetValue(details)!);
    }

    [Fact]
    public void ProductDeletedException_Should_Carry_Trio_With_Product_Code_In_Hint()
    {
        var productId = Guid.NewGuid();
        var ex = new ProductDeletedException(productId, "P-DEL-001");

        AssertDomain.Trio(ex, "PRODUCT_DELETED", "P-DEL-001");
        Assert.Contains("Movimentos não podem ser registrados", ex.Hint!);
        Assert.Equal("BUSINESS_RULE", ex.Category);

        var details = ex.Details!;
        var t = details.GetType();
        Assert.Equal(productId, (Guid)t.GetProperty("productId")!.GetValue(details)!);
        Assert.Equal("P-DEL-001", (string)t.GetProperty("productCode")!.GetValue(details)!);
    }

    [Fact]
    public void ProductNotFoundException_Should_Carry_Trio_With_NOT_FOUND_Category()
    {
        var productId = Guid.NewGuid();
        var ex = new ProductNotFoundException(productId);

        AssertDomain.Trio(ex, "PRODUCT_NOT_FOUND", "não encontrado");
        Assert.Equal("NOT_FOUND", ex.Category);
        Assert.Contains("Atualize", ex.Hint!);

        var details = ex.Details!;
        var t = details.GetType();
        Assert.Equal(productId, (Guid)t.GetProperty("productId")!.GetValue(details)!);
    }

    [Fact]
    public void InvalidMovementValuesException_Should_Carry_Trio_Inbound_With_SaleValue()
    {
        var ex = new InvalidMovementValuesException(MovementType.Inbound, "saleValue", "supplierValue");

        AssertDomain.Trio(ex, "INVALID_MOVEMENT_VALUES", "'Inbound' não aceita o campo 'saleValue'");
        Assert.Contains("requerem o campo 'supplierValue'", ex.Hint!);
        Assert.Equal("BUSINESS_RULE", ex.Category);

        var details = ex.Details!;
        var t = details.GetType();
        Assert.Equal("Inbound", (string)t.GetProperty("type")!.GetValue(details)!);
        Assert.Equal("saleValue", (string)t.GetProperty("providedField")!.GetValue(details)!);
        Assert.Equal("supplierValue", (string)t.GetProperty("expectedField")!.GetValue(details)!);
    }

    [Fact]
    public void InvalidMovementValuesException_Should_Carry_Trio_Outbound_With_SupplierValue()
    {
        var ex = new InvalidMovementValuesException(MovementType.Outbound, "supplierValue", "saleValue");

        AssertDomain.Trio(ex, "INVALID_MOVEMENT_VALUES", "'Outbound' não aceita o campo 'supplierValue'");
        Assert.Contains("requerem o campo 'saleValue'", ex.Hint!);

        var details = ex.Details!;
        var t = details.GetType();
        Assert.Equal("Outbound", (string)t.GetProperty("type")!.GetValue(details)!);
        Assert.Equal("supplierValue", (string)t.GetProperty("providedField")!.GetValue(details)!);
        Assert.Equal("saleValue", (string)t.GetProperty("expectedField")!.GetValue(details)!);
    }

    [Fact]
    public void MovementNotFoundException_Should_Carry_Trio_With_NOT_FOUND_Category()
    {
        var movementId = Guid.NewGuid();
        var ex = new MovementNotFoundException(movementId);

        AssertDomain.Trio(ex, "MOVEMENT_NOT_FOUND", "não encontrado");
        Assert.Equal("NOT_FOUND", ex.Category);
        Assert.Contains("Atualize", ex.Hint!);

        var details = ex.Details!;
        var t = details.GetType();
        Assert.Equal(movementId, (Guid)t.GetProperty("movementId")!.GetValue(details)!);
    }

    [Fact]
    public void MissingIdempotencyKeyException_Should_Carry_Trio_With_VALIDATION_Category()
    {
        var ex = new MissingIdempotencyKeyException();

        AssertDomain.Trio(ex, "MISSING_IDEMPOTENCY_KEY", "Idempotency-Key");
        Assert.Equal("VALIDATION", ex.Category);
        Assert.Contains("crypto.randomUUID()", ex.Hint!);
        Assert.True(ex.Retryable);
    }
}
