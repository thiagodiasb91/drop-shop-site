namespace Dropship.Domain;

/// <summary>
/// Domain para a relação entre Produto e Vendedor (Marketplace)
/// Registro META para busca rápida de produtos por vendedor
/// </summary>
public class ProductSellerDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!;
    public string Sk { get; set; } = default!;

    // Identificadores
    public string EntityType { get; set; } = "product_seller";
    public string ProductId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string SellerId { get; set; } = default!;
    
    // Marketplace
    public string Marketplace { get; set; } = default!;
    public long StoreId { get; set; }

    // Dados
    public int SkuCount { get; set; } // Quantidade de SKUs vinculados
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Mapper para converter Dictionary em Domain
/// </summary>
public static class ProductSellerMapper
{
    public static ProductSellerDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        // Parsear CreatedAt
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

        // Parsear UpdatedAt
        var updatedAtString = item.ContainsKey("updated_at") ? item["updated_at"].S : null;
        DateTime.TryParse(updatedAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var updatedAt);

        return new ProductSellerDomain
        {
            // Chaves
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",

            // Identificadores
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "product_seller",
            ProductId = item.ContainsKey("product_id") ? item["product_id"].S : "",
            ProductName = item.ContainsKey("product_name") ? item["product_name"].S : "",
            SellerId = item.ContainsKey("seller_id") ? item["seller_id"].S : "",

            // Marketplace
            Marketplace = item.ContainsKey("marketplace") ? item["marketplace"].S : "",
            StoreId = item.ContainsKey("store_id") && long.TryParse(item["store_id"].N, out var storeId) ? storeId : 0,

            // Dados
            SkuCount = item.ContainsKey("sku_count") && int.TryParse(item["sku_count"].N, out var count) ? count : 0,

            // Metadata
            CreatedAt = createdAt,
            UpdatedAt = updatedAtString != null ? updatedAt : null
        };
    }

    public static List<ProductSellerDomain> ToDomainList(this List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}

