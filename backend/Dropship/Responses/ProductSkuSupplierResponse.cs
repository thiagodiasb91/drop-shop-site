namespace Dropship.Responses;

/// <summary>
/// Response para a relação Product-SKU-Supplier
/// </summary>
public class ProductSkuSupplierResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public decimal ProductionPrice { get; set; }
    public long Quantity { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Response para lista de Product-SKU-Supplier
/// </summary>
public class ProductSkuSupplierListResponse
{
    public int Total { get; set; }
    public List<ProductSkuSupplierResponse> Items { get; set; } = new();
}

/// <summary>
/// Mapper para converter Domain em Response
/// </summary>
public static class ProductSkuSupplierResponseMapper
{
    public static ProductSkuSupplierResponse ToResponse(this Domain.ProductSkuSupplierDomain domain)
    {
        return new ProductSkuSupplierResponse
        {
            ProductId = domain.ProductId,
            Sku = domain.Sku,
            SupplierId = domain.SupplierId,
            ProductionPrice = domain.ProductionPrice,
            Quantity = domain.Quantity
        };
    }

    public static ProductSkuSupplierListResponse ToListResponse(this List<Domain.ProductSkuSupplierDomain> domains)
    {
        return new ProductSkuSupplierListResponse
        {
            Total = domains.Count,
            Items = domains.Select(d => d.ToResponse()).ToList()
        };
    }
}

