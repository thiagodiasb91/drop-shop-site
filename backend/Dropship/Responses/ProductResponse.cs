using System.Text.Json.Serialization;

namespace Dropship.Responses;

/// <summary>
/// Response completa para informações de um produto
/// </summary>
public class ProductResponse
{
    /// <summary>
    /// ID único do produto
    /// </summary>
    [JsonPropertyName("product_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de entidade
    /// </summary>
    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = "product";

    /// <summary>
    /// Nome do produto
    /// </summary>
    [JsonPropertyName("product_name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data de criação do produto (read-only)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data da última atualização (read-only)
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response simplificada para listagem de produtos
/// </summary>
public class ProductItemResponse
{
    /// <summary>
    /// ID único do produto
    /// </summary>
    [JsonPropertyName("product_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nome do produto
    /// </summary>
    [JsonPropertyName("product_name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data de criação do produto
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response com paginação para listagem de produtos
/// </summary>
public class ProductListResponse
{
    /// <summary>
    /// Total de produtos
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Lista de produtos
    /// </summary>
    [JsonPropertyName("items")]
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
            EntityType = product.EntityType,
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
