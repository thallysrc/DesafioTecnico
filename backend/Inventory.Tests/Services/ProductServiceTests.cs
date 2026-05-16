using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Exceptions;
using Inventory.Api.Infra;
using Inventory.Api.Repositories;
using Inventory.Api.Services;
using Inventory.Tests.TestHelpers;
using Moq;
using Npgsql;

namespace Inventory.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProductService"/> — covers happy paths (Create / Get / List / SoftDelete),
/// page/pageSize clamping, and the two typed-exception paths the service can produce on its own
/// (<see cref="DuplicateCodeException"/> + <see cref="ProductNotFoundException"/>).
///
/// Mocks only <see cref="IProductRepository"/> — no Postgres + no Testcontainers (CONTEXT.md D-01).
/// Every exception assertion goes through <see cref="AssertDomain.Trio"/> to lock the
/// (ErrorCode, Message substring, non-null Hint) trio required by TEST-03 / CONTEXT.md D-03.
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repo = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repo.Object);
    }

    private static Product ValidProduct(Guid? id = null, DateTime? deletedAt = null) => new()
    {
        Id = id ?? Guid.Parse("7168c579-8259-4c0e-949b-998a7147c47f"),
        Code = "P001",
        Description = "Notebook",
        Type = ProductType.Electronic,
        SupplierValue = 100m,
        StockQuantity = 5,
        DeletedAt = deletedAt,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static CreateProductRequest ValidRequest() =>
        new("P001", "Notebook", ProductType.Electronic, 100m, 5);

    // ----- Happy paths ---------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Should_Return_Mapped_ProductResponse_When_Repository_Succeeds()
    {
        var created = ValidProduct();
        _repo.Setup(r => r.CreateAsync(It.IsAny<Product>())).ReturnsAsync(created);

        var response = await _sut.CreateAsync(ValidRequest());

        Assert.Equal(created.Id, response.Id);
        Assert.Equal("P001", response.Code);
        Assert.Equal("Notebook", response.Description);
        Assert.Equal(ProductType.Electronic, response.Type);
        Assert.Equal(100m, response.SupplierValue);
        Assert.Equal(5, response.StockQuantity);
        Assert.Null(response.DeletedAt);
        Assert.NotNull(response.Links);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_ProductResponse_When_Found()
    {
        var product = ValidProduct();
        _repo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var response = await _sut.GetByIdAsync(product.Id);

        Assert.Equal(product.Id, response.Id);
        Assert.Equal(product.Code, response.Code);
        Assert.Null(response.DeletedAt);
        Assert.NotNull(response.Links);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_ProductResponse_With_DeletedAt_When_SoftDeleted()
    {
        var deletedAt = DateTime.UtcNow.AddMinutes(-5);
        var product = ValidProduct(deletedAt: deletedAt);
        _repo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var response = await _sut.GetByIdAsync(product.Id);

        Assert.Equal(deletedAt, response.DeletedAt);
        Assert.NotNull(response.Links);
    }

    [Fact]
    public async Task ListAsync_Should_Clamp_Page_To_Minimum_1()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
             .ReturnsAsync(new PagedResult<Product>(new List<Product>(), 1, 30, 0));

        await _sut.ListAsync(page: 0, pageSize: 30, includeDeleted: false);

        _repo.Verify(r => r.ListAsync(1, 30, false), Times.Once);
    }

    [Fact]
    public async Task ListAsync_Should_Clamp_PageSize_To_Maximum_100()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
             .ReturnsAsync(new PagedResult<Product>(new List<Product>(), 1, 100, 0));

        await _sut.ListAsync(page: 1, pageSize: 9999, includeDeleted: false);

        _repo.Verify(r => r.ListAsync(1, 100, false), Times.Once);
    }

    [Fact]
    public async Task ListAsync_Should_Default_PageSize_To_30_When_Zero()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
             .ReturnsAsync(new PagedResult<Product>(new List<Product>(), 1, 30, 0));

        await _sut.ListAsync(page: 1, pageSize: 0, includeDeleted: false);

        _repo.Verify(r => r.ListAsync(1, 30, false), Times.Once);
    }

    [Fact]
    public async Task ListAsync_Should_Return_PaginationMeta_With_TotalPages_Computed()
    {
        var items = new List<Product> { ValidProduct() };
        _repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
             .ReturnsAsync(new PagedResult<Product>(items, 1, 30, 61));

        var response = await _sut.ListAsync(page: 1, pageSize: 30, includeDeleted: false);

        Assert.Equal(1, response.Pagination.Page);
        Assert.Equal(30, response.Pagination.PageSize);
        Assert.Equal(61, response.Pagination.Total);
        Assert.Equal(3, response.Pagination.TotalPages); // ceil(61/30) == 3
        Assert.True(response.Pagination.HasNext);
        Assert.False(response.Pagination.HasPrev);
        Assert.Single(response.Items);
        Assert.NotNull(response.Links);
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Complete_Silently_When_Repository_Affects_One_Row()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.SoftDeleteAsync(id)).ReturnsAsync(1);

        // Should NOT throw.
        await _sut.SoftDeleteAsync(id);

        _repo.Verify(r => r.SoftDeleteAsync(id), Times.Once);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ----- Exception paths -----------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Should_Throw_DUPLICATE_CODE_When_Postgres_23505_Bubbles()
    {
        var pgex = new PostgresException(
            messageText: "duplicate key value violates unique constraint \"products_code_key\"",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: "23505");
        _repo.Setup(r => r.CreateAsync(It.IsAny<Product>())).ThrowsAsync(pgex);

        var ex = await Assert.ThrowsAsync<DuplicateCodeException>(
            () => _sut.CreateAsync(ValidRequest()));

        AssertDomain.Trio(ex, "DUPLICATE_CODE", "código 'P001'");
        Assert.Contains("P001", ex.Message);
        Assert.NotNull(ex.Hint);
        Assert.Contains("P001", ex.Hint!);
        Assert.Equal("BUSINESS_RULE", ex.Category);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Throw_PRODUCT_NOT_FOUND_When_Repository_Returns_Null()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Product?)null);

        var ex = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => _sut.GetByIdAsync(id));

        AssertDomain.Trio(ex, "PRODUCT_NOT_FOUND", "não encontrado");
        Assert.Contains("Atualize", ex.Hint!);
        Assert.Equal("NOT_FOUND", ex.Category);
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Throw_PRODUCT_NOT_FOUND_When_Affected_Zero_And_Product_Missing()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.SoftDeleteAsync(id)).ReturnsAsync(0);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Product?)null);

        var ex = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => _sut.SoftDeleteAsync(id));

        AssertDomain.Trio(ex, "PRODUCT_NOT_FOUND", "não encontrado");
        Assert.Equal("NOT_FOUND", ex.Category);
        _repo.Verify(r => r.SoftDeleteAsync(id), Times.Once);
        _repo.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_Should_Throw_PRODUCT_NOT_FOUND_When_Affected_Zero_And_Product_Already_Deleted()
    {
        var id = Guid.NewGuid();
        // Conflated path per ProductService.cs lines 99-103: even when the product exists but is
        // already soft-deleted, surface as PRODUCT_NOT_FOUND (identical UX — the product is no
        // longer actionable).
        var alreadyDeleted = ValidProduct(id: id, deletedAt: DateTime.UtcNow.AddMinutes(-1));
        _repo.Setup(r => r.SoftDeleteAsync(id)).ReturnsAsync(0);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(alreadyDeleted);

        var ex = await Assert.ThrowsAsync<ProductNotFoundException>(
            () => _sut.SoftDeleteAsync(id));

        AssertDomain.Trio(ex, "PRODUCT_NOT_FOUND", "não encontrado");
        Assert.NotNull(ex.Hint);
    }
}
