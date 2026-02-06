using Amazon.DynamoDBv2.Model;

namespace Dropship.Domain;

/// <summary>
/// Domínio para Produto
/// Representa um produto cadastrado no sistema
/// </summary>
public class ProductDomain
{
    // Chaves DynamoDB
    public string PK { get; set; } = default!;
    public string SK { get; set; } = default!;

    // Identificadores
    public string Id { get; set; } = default!;
    public string EntityType { get; set; } = "product";

    // Informações do Produto
    public string Name { get; set; } = default!;

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Mapper para converter DynamoDB items para ProductDomain
/// </summary>
public static class ProductMapper
{
    public static ProductDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        // Parsear CreatedAt
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

        // Parsear UpdatedAt
        var updatedAtString = item.ContainsKey("updated_at") ? item["updated_at"].S : null;
        DateTime.TryParse(updatedAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var updatedAt);

        return new ProductDomain
        {
            // Chaves
            PK = item.ContainsKey("PK") ? item["PK"].S : "",
            SK = item.ContainsKey("SK") ? item["SK"].S : "",

            // Identificadores
            Id = item.ContainsKey("product_id") ? item["product_id"].S : 
                 (item.ContainsKey("id") ? item["id"].S : ""),
            EntityType = item.ContainsKey("entityType") ? item["entityType"].S : "product",

            // Informações
            Name = item.ContainsKey("product_name") ? item["product_name"].S : 
                   (item.ContainsKey("name") ? item["name"].S : ""),

            // Metadata
            CreatedAt = createdAt,
            UpdatedAt = updatedAtString != null ? updatedAt : null
        };
    }
}
