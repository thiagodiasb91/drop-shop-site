using System.Globalization;
using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

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
    public string SupplierId { get; set; } = string.Empty; // ID do fornecedor (opcional)

    public string Color { get; set; } = default!;
    public string Size { get; set; } = default!;
    // Marketplace
    public string Marketplace { get; set; } = default!; // ex: "shopee", "mercado_livre"
    public long StoreId { get; set; } // ID da loja no marketplace (ex: shop_id Shopee)
    public string MarketplaceItemId { get; set; } = string.Empty; // ID do produto no marketplace
    public string MarketplaceModelId { get; set; } = string.Empty; // ID do modelo/SKU no marketplace

    // Dados
    public decimal Price { get; set; } // Preço definido pelo vendedor
    public long Quantity { get; set; } // Quantidade atualizada automaticamente via sistema

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Factory para criar instâncias de ProductSkuSellerDomain
/// Centraliza a lógica de criação com geração automática de chaves PK/SK
/// </summary>
public static class ProductSkuSellerFactory
{
    public static ProductSkuSellerDomain Create(
        string productId,
        string sku,
        string sellerId,
        string marketplace,
        long storeId,
        decimal price,
        string color,
        string size,
        string supplierId)
    {
        return new ProductSkuSellerDomain
        {
            Pk = $"Product#{productId}",
            Sk = $"Sku#{sku}#Seller#{marketplace}#{sellerId}",
            EntityType = "product_sku_seller",
            ProductId = productId,
            Sku = sku,
            SellerId = sellerId,
            SupplierId = supplierId,
            Marketplace = marketplace,
            StoreId = storeId,
            Price = price,
            Quantity = 0,
            Color = color,
            Size = size,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }
}

/// <summary>
/// Mapper para converter Dictionary em Domain
/// </summary>
public static class ProductSkuSellerMapper
{
    public static ProductSkuSellerDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new ProductSkuSellerDomain
        {
            Pk                 = item.GetS("PK"),
            Sk                 = item.GetS("SK"),
            EntityType         = item.GetS("entity_type", "product_sku_seller"),
            ProductId          = item.GetS("product_id"),
            Sku                = item.GetS("sku"),
            SellerId           = item.GetS("seller_id"),
            SupplierId         = item.GetS("supplier_id"),
            Marketplace        = item.GetS("marketplace"),
            StoreId            = item.GetN<long>("store_id"),
            MarketplaceItemId  = item.GetS("marketplace_item_id"),
            MarketplaceModelId = item.GetS("marketplace_model_id"),
            Price              = item.GetDecimal("price"),
            Quantity           = item.GetN<long>("quantity"),
            CreatedAt          = item.GetDateTimeS("created_at"),
            UpdatedAt          = item.GetDateTimeSNullable("updated_at"),
        };
    }

    /// <summary>
    /// Converte ProductSkuSellerDomain para Dictionary pronto para salvar no DynamoDB
    /// </summary>
    public static Dictionary<string, AttributeValue> ToDynamoDb(this ProductSkuSellerDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.Pk } },
            { "SK", new AttributeValue { S = domain.Sk } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "product_id", new AttributeValue { S = domain.ProductId } },
            { "sku", new AttributeValue { S = domain.Sku } },
            { "supplier_id", new AttributeValue { S = domain.SupplierId } },
            { "seller_id", new AttributeValue { S = domain.SellerId } },
            { "marketplace", new AttributeValue { S = domain.Marketplace } },
            { "store_id", new AttributeValue { N = domain.StoreId.ToString(CultureInfo.InvariantCulture) } },
            { "marketplace_item_id", new AttributeValue { S = domain.MarketplaceItemId } },
            { "marketplace_model_id", new AttributeValue { S = domain.MarketplaceModelId } },
            { "price", new AttributeValue { N = domain.Price.ToString("0.00", CultureInfo.InvariantCulture) } },
            { "quantity", new AttributeValue { N = domain.Quantity.ToString(CultureInfo.InvariantCulture) } },
            { "color", new AttributeValue { S = domain.Color } },
            { "size", new AttributeValue { S = domain.Size } },
            { "created_at", new AttributeValue { S = domain.CreatedAt.ToString("O") } }
        };

        // Adicionar updated_at se fornecido
        if (domain.UpdatedAt.HasValue)
        {
            item["updated_at"] = new AttributeValue { S = domain.UpdatedAt.Value.ToString("O") };
        }

        return item;
    }

    public static List<ProductSkuSellerDomain> ToDomainList(this List<Dictionary<string, AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}
