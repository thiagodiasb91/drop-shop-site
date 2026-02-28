using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

/// <summary>
/// Domain para a relação entre Produto e Fornecedor
/// Permite buscar produtos por fornecedor de forma eficiente
/// Preços são armazenados em centavos (cents)
/// </summary>
public class ProductSupplierDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!;
    public string Sk { get; set; } = default!;

    // Identificadores
    public string EntityType { get; set; } = "product_supplier";
    public string ProductId { get; set; } = default!;
    public string SupplierId { get; set; } = default!;

    public string ProductName { get; set; } = default!;
    public int SkuCount { get; set; } // Quantidade de SKUs fornecidos
    
    // 💰 Preços em centavos (populados automaticamente pelos SKUs)
    public decimal MinPrice { get; set; } // Preço mínimo dos SKUs
    public decimal MaxPrice { get; set; } // Preço máximo dos SKUs
    
    public DateTime CreatedAt { get; set; } // Data de criação
}

/// <summary>
/// Mapper para converter Dictionary em Domain
/// </summary>
public static class ProductSupplierMapper
{
    public static ProductSupplierDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new ProductSupplierDomain
        {
            Pk          = item.GetS("PK"),
            Sk          = item.GetS("SK"),
            EntityType  = item.GetS("entity_type", "product_supplier"),
            ProductId   = item.GetS("product_id"),
            SupplierId  = item.GetS("supplier_id"),
            ProductName = item.GetS("product_name"),
            SkuCount    = item.GetN<int>("sku_count"),
            MinPrice    = item.GetDecimal("min_price"),
            MaxPrice    = item.GetDecimal("max_price"),
            CreatedAt   = item.GetDateTimeS("created_at"),
        };
    }

    public static List<ProductSupplierDomain> ToDomainList(this List<Dictionary<string, AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}
