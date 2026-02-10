namespace Dropship.Responses;

/// <summary>
/// Response para a relação Product-SKU-Seller
/// </summary>
public class ProductSkuSellerResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public string Marketplace { get; set; } = string.Empty;
    public long StoreId { get; set; }
    public string MarketplaceProductId { get; set; } = string.Empty;
    public string MarketplaceModelId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public long Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response para lista de Product-SKU-Seller
/// </summary>
public class ProductSkuSellerListResponse
{
    public int Total { get; set; }
    public List<ProductSkuSellerResponse> Items { get; set; } = new();
}

/// <summary>
/// Response para Product-Seller META
/// </summary>
public class ProductSellerResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public string Marketplace { get; set; } = string.Empty;
    public long StoreId { get; set; }
    public int SkuCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response para lista de Product-Seller
/// </summary>
public class ProductSellerListResponse
{
    public int Total { get; set; }
    public List<ProductSellerResponse> Items { get; set; } = new();
}

/// <summary>
/// Mapper para Product-SKU-Seller
/// </summary>
public static class ProductSkuSellerResponseMapper
{
    public static ProductSkuSellerResponse ToResponse(this Domain.ProductSkuSellerDomain domain)
    {
        return new ProductSkuSellerResponse
        {
            ProductId = domain.ProductId,
            Sku = domain.Sku,
            SellerId = domain.SellerId,
            Marketplace = domain.Marketplace,
            StoreId = domain.StoreId,
            MarketplaceProductId = domain.MarketplaceProductId,
            MarketplaceModelId = domain.MarketplaceModelId,
            Price = domain.Price,
            Quantity = domain.Quantity,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }

    public static ProductSkuSellerListResponse ToListResponse(this List<Domain.ProductSkuSellerDomain> domains)
    {
        return new ProductSkuSellerListResponse
        {
            Total = domains.Count,
            Items = domains.Select(d => d.ToResponse()).ToList()
        };
    }
}

/// <summary>
/// Mapper para Product-Seller
/// </summary>
public static class ProductSellerResponseMapper
{
    public static ProductSellerResponse ToResponse(this Domain.ProductSellerDomain domain)
    {
        return new ProductSellerResponse
        {
            ProductId = domain.ProductId,
            ProductName = domain.ProductName,
            SellerId = domain.SellerId,
            Marketplace = domain.Marketplace,
            StoreId = domain.StoreId,
            SkuCount = domain.SkuCount,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }

    public static ProductSellerListResponse ToListResponse(this List<Domain.ProductSellerDomain> domains)
    {
        return new ProductSellerListResponse
        {
            Total = domains.Count,
            Items = domains.Select(d => d.ToResponse()).ToList()
        };
    }
}

