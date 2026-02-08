using Amazon.DynamoDBv2.Model;

namespace Dropship.Domain;

/// <summary>
/// Domínio para SKU (Stock Keeping Unit)
/// Representa uma variação de um produto com atributos como tamanho, cor e quantidade
/// </summary>
public class SkuDomain
{
    // Chaves DynamoDB
    public string PK { get; set; } = default!;
    public string SK { get; set; } = default!;

    // Identificadores
    public string Id { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public string EntityType { get; set; } = "sku";

    // Informações do SKU
    public string Sku { get; set; } = default!;
    public string Size { get; set; } = default!;
    public string Color { get; set; } = default!;
    public int Quantity { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Mapper para converter DynamoDB items para SkuDomain
/// </summary>
public static class SkuMapper
{
    public static SkuDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        // Parsear CreatedAt
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

        // Parsear UpdatedAt
        var updatedAtString = item.ContainsKey("updated_at") ? item["updated_at"].S : null;
        DateTime.TryParse(updatedAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var updatedAt);

        // Parsear Quantity
        var quantityString = item.ContainsKey("quantity") ? item["quantity"].N : "0";
        int.TryParse(quantityString, out var quantity);

        return new SkuDomain
        {
            // Chaves
            PK = item.ContainsKey("PK") ? item["PK"].S : "",
            SK = item.ContainsKey("SK") ? item["SK"].S : "",

            // Identificadores
            Id = item.ContainsKey("sku") ? item["sku"].S : "",
            ProductId = item.ContainsKey("productId") ? item["productId"].S : 
                       (item.ContainsKey("product_id") ? item["product_id"].S : ""),
            EntityType = item.ContainsKey("entityType") ? item["entityType"].S : "sku",

            // Informações
            Sku = item.ContainsKey("sku") ? item["sku"].S : "",
            Size = item.ContainsKey("size") ? item["size"].S : "",
            Color = item.ContainsKey("color") ? item["color"].S : "",
            Quantity = quantity,

            // Metadata
            CreatedAt = createdAt,
            UpdatedAt = updatedAtString != null ? updatedAt : null
        };
    }
}
