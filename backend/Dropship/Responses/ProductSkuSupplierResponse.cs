using Dropship.Domain;

namespace Dropship.Responses;

/// <summary>
/// Response para a relação Product-SKU-Supplier
/// </summary>
public class ProductSkuSupplierResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public long Quantity { get; set; }
    public string SkuSupplier { get; set; }
    public string SupplierName { get; set; } = string.Empty;
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
    public static ProductSkuSupplierResponse ToResponse(this Domain.ProductSkuSupplierDomain domain, SupplierDomain supplierDomain)
    {
        return new ProductSkuSupplierResponse
        {
            ProductId = domain.ProductId,
            Sku = domain.Sku,
            SupplierId = supplierDomain.Id,
            SupplierName = supplierDomain.Name,
            Price = domain.Price,
            Quantity = domain.Quantity,
            SkuSupplier = domain.SkuSupplier
        };
    }

    public static ProductSkuSupplierListResponse ToListResponse(this List<Domain.ProductSkuSupplierDomain> domains, List<SupplierDomain> supplierDomains)
    {
        return new ProductSkuSupplierListResponse
        {
            Total = domains.Count,
            Items = domains.Select(d => d.ToResponse( supplierDomains.First( s => s.Id == d.SupplierId))).ToList()
        };
    }
}

