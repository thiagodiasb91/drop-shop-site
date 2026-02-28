using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

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
        return new SkuDomain
        {
            PK        = item.GetS("PK"),
            SK        = item.GetS("SK"),
            Id        = item.GetS("sku"),
            ProductId = item.GetS("product_id"),
            EntityType= item.GetS("entity_type", "sku"),
            Sku       = item.GetS("sku"),
            Size      = item.GetS("size"),
            Color     = item.GetS("color"),
            Quantity  = item.GetN<int>("quantity"),
            CreatedAt = item.GetDateTimeS("created_at"),
            UpdatedAt = item.GetDateTimeSNullable("updated_at"),
        };
    }
}
