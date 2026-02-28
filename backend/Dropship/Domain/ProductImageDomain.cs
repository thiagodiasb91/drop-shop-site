using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

/// <summary>
/// Domain para imagens de produtos
/// Armazena URLs e metadados de imagens vinculadas a um produto
/// </summary>
public class ProductImageDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!;
    public string Sk { get; set; } = default!;

    // Identificadores
    public string EntityType { get; set; } = "product_image";
    public string ProductId { get; set; } = default!;
    public string ImageId { get; set; } = default!;

    // Atributos
    public string Color { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty; // Shopee image ID
    public string BrUrl { get; set; } = string.Empty; // Brazil URL

    // Metadata
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Factory para criar instâncias de ProductImageDomain
/// </summary>
public static class ProductImageFactory
{
    public static ProductImageDomain Create(
        string productId,
        string color,
        string imageId,
        string imageUrl,
        string brUrl = "")
    {
        return new ProductImageDomain
        {
            Pk = $"Product#{productId}",
            Sk = $"Color#{color}#Image#{imageId}",
            EntityType = "product_image",
            ProductId = productId,
            Color = color,
            ImageId = imageId,
            ImageUrl = imageUrl,
            BrUrl = brUrl,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Mapper para converter DynamoDB items para ProductImageDomain
/// </summary>
public static class ProductImageMapper
{
    public static ProductImageDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new ProductImageDomain
        {
            Pk         = item.GetS("PK"),
            Sk         = item.GetS("SK"),
            EntityType = item.GetS("entity_type", "product_image"),
            ProductId  = item.GetS("product_id"),
            ImageId    = item.GetS("image"),
            Color      = item.GetS("color"),
            ImageUrl   = item.GetS("image"),
            BrUrl      = item.GetS("br_url"),
            CreatedAt  = item.GetDateTimeS("created_at"),
        };
    }

    public static List<ProductImageDomain> ToDomainList(this List<Dictionary<string, AttributeValue>> items)
        => items.Select(ToDomain).ToList();
}
