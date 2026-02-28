using System.Globalization;
using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

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
    public string SupplierId { get; set; } = string.Empty; // ID do fornecedor (opcional)
    
    // Marketplace
    public string Marketplace { get; set; } = default!;
    public long StoreId { get; set; }
    public long MarketplaceItemId { get; set; }
    public decimal Price { get; set; }

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
    public static ProductSellerDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new ProductSellerDomain
        {
            Pk                = item.GetS("PK"),
            Sk                = item.GetS("SK"),
            EntityType        = item.GetS("entity_type", "product_seller"),
            ProductId         = item.GetS("product_id"),
            ProductName       = item.GetS("product_name"),
            SellerId          = item.GetS("seller_id"),
            SupplierId        = item.GetS("supplier_id"),
            Marketplace       = item.GetS("marketplace"),
            StoreId           = item.GetN<long>("store_id"),
            MarketplaceItemId = item.GetN<long>("marketplace_item_id"),
            Price             = item.GetDecimal("price"),
            SkuCount          = item.GetN<int>("sku_count"),
            CreatedAt         = item.GetDateTimeS("created_at"),
            UpdatedAt         = item.GetDateTimeSNullable("updated_at"),
        };
    }

    /// <summary>
    /// Converte ProductSellerDomain para Dictionary pronto para salvar no DynamoDB
    /// </summary>
    public static Dictionary<string, AttributeValue> ToDynamoDb(this ProductSellerDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.Pk } },
            { "SK", new AttributeValue { S = domain.Sk } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "product_id", new AttributeValue { S = domain.ProductId } },
            { "product_name", new AttributeValue { S = domain.ProductName } },
            { "seller_id", new AttributeValue { S = domain.SellerId } },
            { "supplier_id", new AttributeValue { S = domain.SupplierId } },
            { "marketplace", new AttributeValue { S = domain.Marketplace } },
            { "store_id", new AttributeValue { N = domain.StoreId.ToString(CultureInfo.InvariantCulture) } },
            { "marketplace_item_id", new AttributeValue { N = domain.MarketplaceItemId.ToString(CultureInfo.InvariantCulture) } },
            { "price", new AttributeValue { N = domain.Price.ToString("0.00", CultureInfo.InvariantCulture) } },
            { "sku_count", new AttributeValue { N = domain.SkuCount.ToString(CultureInfo.InvariantCulture) } },
            { "created_at", new AttributeValue { S = domain.CreatedAt.ToString("O") } },
        };

        // Adicionar updated_at se fornecido
        if (domain.UpdatedAt.HasValue)
        {
            item["updated_at"] = new AttributeValue { S = domain.UpdatedAt.Value.ToString("O") };
        }

        return item;
    }

    public static List<ProductSellerDomain> ToDomainList(this List<Dictionary<string, AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}
