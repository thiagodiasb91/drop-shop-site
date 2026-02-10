using System.Text.Json.Serialization;

namespace Dropship.Responses;

public class ProductResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ProductListResponse
{
    public int Total { get; set; }
    public List<ProductItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Mapper para converter ProductDomain em Response
/// </summary>
public static class ProductResponseMapper
{
    public static ProductResponse ToResponse(this Domain.ProductDomain product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public static ProductItemResponse ToItemResponse(this Domain.ProductDomain product)
    {
        return new ProductItemResponse
        {
            Id = product.Id,
            Name = product.Name,
            CreatedAt = product.CreatedAt
        };
    }

    public static ProductListResponse ToListResponse(this List<Domain.ProductDomain> products)
    {
        return new ProductListResponse
        {
            Total = products.Count,
            Items = products.Select(p => p.ToItemResponse()).ToList()
        };
    }

    public static List<ProductResponse> ToResponse(this List<Domain.ProductDomain> products)
    {
        return products.Select(p => p.ToResponse()).ToList();
    }
}
