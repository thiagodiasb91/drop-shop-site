using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

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
    public string Description { get; set; } = default!;
    public string ImageUrl { get; set; } = default!;

    public int CategoryId { get; set; } = default!;
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
        return new ProductDomain
        {
            PK          = item.GetS("PK"),
            SK          = item.GetS("SK"),
            Id          = item.GetS("product_id"),
            EntityType  = item.GetS("entity_type", "product"),
            Name        = item.GetS("product_name"),
            Description = item.GetS("product_description"),
            ImageUrl    = item.GetS("product_image_url"),
            CategoryId  = item.GetN<int>("product_category_id"),
            CreatedAt   = item.GetDateTimeS("created_at"),
            UpdatedAt   = item.GetDateTimeSNullable("updated_at"),
        };
    }
}
