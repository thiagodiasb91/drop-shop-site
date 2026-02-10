namespace Dropship.Domain;

/// <summary>
/// Domain para a relação entre Produto, SKU e Vendedor (Marketplace)
/// Armazena informações de preço e dados do marketplace
/// A quantidade é atualizada automaticamente via sistema
/// </summary>
public class ProductSkuSellerDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!;
    public string Sk { get; set; } = default!;

    // Identificadores
    public string EntityType { get; set; } = "product_sku_seller";
    public string ProductId { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string SellerId { get; set; } = default!;
    
    // Marketplace
    public string Marketplace { get; set; } = default!; // ex: "shopee", "mercado_livre"
    public long StoreId { get; set; } // ID da loja no marketplace (ex: shop_id Shopee)
    public string MarketplaceProductId { get; set; } = string.Empty; // ID do produto no marketplace
    public string MarketplaceModelId { get; set; } = string.Empty; // ID do modelo/SKU no marketplace

    // Dados
    public decimal Price { get; set; } // Preço definido pelo vendedor
    public long Quantity { get; set; } // Quantidade atualizada automaticamente via sistema

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Mapper para converter Dictionary em Domain
/// </summary>
public static class ProductSkuSellerMapper
{
    public static ProductSkuSellerDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        // Parsear CreatedAt
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);

        // Parsear UpdatedAt
        var updatedAtString = item.ContainsKey("updated_at") ? item["updated_at"].S : null;
        DateTime.TryParse(updatedAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var updatedAt);

        return new ProductSkuSellerDomain
        {
            // Chaves
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",

            // Identificadores
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "product_sku_seller",
            ProductId = item.ContainsKey("product_id") ? item["product_id"].S : "",
            Sku = item.ContainsKey("sku") ? item["sku"].S : "",
            SellerId = item.ContainsKey("seller_id") ? item["seller_id"].S : "",

            // Marketplace
            Marketplace = item.ContainsKey("marketplace") ? item["marketplace"].S : "",
            StoreId = item.ContainsKey("store_id") && long.TryParse(item["store_id"].N, out var storeId) ? storeId : 0,
            MarketplaceProductId = item.ContainsKey("marketplace_product_id") ? item["marketplace_product_id"].S : "",
            MarketplaceModelId = item.ContainsKey("marketplace_model_id") ? item["marketplace_model_id"].S : "",

            // Dados
            Price = item.ContainsKey("price") && decimal.TryParse(item["price"].N, System.Globalization.CultureInfo.InvariantCulture, out var price) ? price : 0,
            Quantity = item.ContainsKey("quantity") && long.TryParse(item["quantity"].N, out var qty) ? qty : 0,

            // Metadata
            CreatedAt = createdAt,
            UpdatedAt = updatedAtString != null ? updatedAt : null
        };
    }

    public static List<ProductSkuSellerDomain> ToDomainList(this List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}

