namespace Dropship.Domain;

/// <summary>
/// Domain para a relação entre Produto e Fornecedor
/// Permite buscar produtos por fornecedor de forma eficiente
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
    public DateTime CreatedAt { get; set; } // Data de criação
}

/// <summary>
/// Mapper para converter Dictionary em Domain
/// </summary>
public static class ProductSupplierMapper
{
    public static ProductSupplierDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        // Parsear CreatedAt - formato ISO 8601
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

        return new ProductSupplierDomain
        {
            // Chaves
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",

            // Identificadores
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "product_supplier",
            ProductId = item.ContainsKey("product_id") ? item["product_id"].S : "",
            SupplierId = item.ContainsKey("supplier_id") ? item["supplier_id"].S : "",
            ProductName = item.ContainsKey("product_name") ? item["product_name"].S : "",

            SkuCount = item.ContainsKey("sku_count") && int.TryParse(item["sku_count"].N, out var count) ? count : 0,
            CreatedAt = createdAt
        };
    }

    public static List<ProductSupplierDomain> ToDomainList(this List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}

