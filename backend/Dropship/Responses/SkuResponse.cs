using System.Text.Json.Serialization;

namespace Dropship.Responses;

/// <summary>
/// Response completa para informações de um SKU
/// </summary>
public class SkuResponse
{
    /// <summary>
    /// ID do SKU (código SKU)
    /// </summary>
    [JsonPropertyName("sku")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID do produto ao qual o SKU pertence
    /// </summary>
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de entidade
    /// </summary>
    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = "sku";

    /// <summary>
    /// Tamanho do produto
    /// </summary>
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Cor do produto
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Data de criação do SKU (read-only)
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
/// Response simplificada para listagem de SKUs
/// </summary>
public class SkuItemResponse
{
    /// <summary>
    /// ID do SKU (código SKU)
    /// </summary>
    [JsonPropertyName("sku")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tamanho do produto
    /// </summary>
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Cor do produto
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Data de criação do SKU
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response com paginação para listagem de SKUs
/// </summary>
public class SkuListResponse
{
    /// <summary>
    /// Total de SKUs
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Lista de SKUs
    /// </summary>
    [JsonPropertyName("items")]
    public List<SkuItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Mapper para converter SkuDomain em Response
/// </summary>
public static class SkuResponseMapper
{
    public static SkuResponse ToResponse(this Domain.SkuDomain sku)
    {
        return new SkuResponse
        {
            Id = sku.Sku,
            ProductId = sku.ProductId,
            EntityType = sku.EntityType,
            Size = sku.Size,
            Color = sku.Color,
            Quantity = sku.Quantity,
            CreatedAt = sku.CreatedAt,
            UpdatedAt = sku.UpdatedAt
        };
    }

    public static SkuItemResponse ToItemResponse(this Domain.SkuDomain sku)
    {
        return new SkuItemResponse
        {
            Id = sku.Sku,
            Size = sku.Size,
            Color = sku.Color,
            Quantity = sku.Quantity,
            CreatedAt = sku.CreatedAt
        };
    }

    public static SkuListResponse ToListResponse(this List<Domain.SkuDomain> skus)
    {
        return new SkuListResponse
        {
            Total = skus.Count,
            Items = skus.Select(s => s.ToItemResponse()).ToList()
        };
    }

    public static List<SkuResponse> ToResponse(this List<Domain.SkuDomain> skus)
    {
        return skus.Select(s => s.ToResponse()).ToList();
    }
}
