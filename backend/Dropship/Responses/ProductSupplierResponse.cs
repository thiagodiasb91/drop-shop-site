namespace Dropship.Responses;

/// <summary>
/// Response para a relação Product-Supplier
/// </summary>
public class ProductSupplierResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public int SkuCount { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response para lista de Product-Supplier
/// </summary>
public class ProductSupplierListResponse
{
    public int Total { get; set; }
    public List<ProductSupplierResponse> Items { get; set; } = new();
}

/// <summary>
/// Mapper para converter Domain em Response
/// </summary>
public static class ProductSupplierResponseMapper
{
    public static ProductSupplierResponse ToResponse(this Domain.ProductSupplierDomain domain)
    {
        return new ProductSupplierResponse
        {
            ProductId = domain.ProductId,
            ProductName = domain.ProductName,
            SupplierId = domain.SupplierId,
            SkuCount = domain.SkuCount,
            CreatedAt = domain.CreatedAt
        };
    }

    public static ProductSupplierListResponse ToListResponse(this List<Domain.ProductSupplierDomain> domains)
    {
        return new ProductSupplierListResponse
        {
            Total = domains.Count,
            Items = domains.Select(d => d.ToResponse()).ToList()
        };
    }
}

