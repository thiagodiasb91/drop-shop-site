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
    public static ProductImageDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        // Parsear CreatedAt
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

        return new ProductImageDomain
        {
            // Chaves
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",

            // Identificadores
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "product_image",
            ProductId = item.ContainsKey("product_id") ? item["product_id"].S : "",
            ImageId = item.ContainsKey("image") ? item["image"].S : "",

            // Atributos
            Color = item.ContainsKey("color") ? item["color"].S : "",
            ImageUrl = item.ContainsKey("image") ? item["image"].S : "",
            BrUrl = item.ContainsKey("br_url") ? item["br_url"].S : "",

            // Metadata
            CreatedAt = createdAt
        };
    }

    public static List<ProductImageDomain> ToDomainList(this List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}

