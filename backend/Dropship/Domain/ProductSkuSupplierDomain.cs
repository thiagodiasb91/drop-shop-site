using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

/// <summary>
/// Domain para a relação entre Produto, SKU e Fornecedor
/// Armazena informações de preço de produção e quantidade disponível por fornecedor
/// </summary>
public class ProductSkuSupplierDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!;
    public string Sk { get; set; } = default!;

    public string EntityType { get; set; } = "product_sku_supplier";
    public string ProductId { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string SkuSupplier { get; set; } = default!;
    public string SupplierId { get; set; } = default!;

    public decimal Price { get; set; }
    public long Quantity { get; set; }  
}

public static class ProductSkuSupplierBuilder
{
    public static ProductSkuSupplierDomain Create(string productId, string sku, string skuSupplier, string supplierId, decimal productionPrice)
    {
        return new ProductSkuSupplierDomain()
        {
            Pk = $"Product#{productId}",
            Sk = $"Sku#{sku}#Supplier#{supplierId}",
            EntityType = "product_sku_supplier",
            ProductId = productId,
            Sku = sku,
            SkuSupplier = skuSupplier,
            SupplierId = supplierId,
            Price = productionPrice,
            Quantity = 0
        };
    }
}

/// <summary>
/// Mapper para converter Dictionary em Domain
/// </summary>
public static class ProductSkuSupplierMapper
{
    public static ProductSkuSupplierDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new ProductSkuSupplierDomain
        {
            Pk          = item.GetS("PK"),
            Sk          = item.GetS("SK"),
            EntityType  = item.GetS("entity_type", "product_sku_supplier"),
            ProductId   = item.GetS("product_id"),
            Sku         = item.GetS("sku"),
            SupplierId  = item.GetS("supplier_id"),
            SkuSupplier = item.GetS("sku_supplier"),
            Price       = item.GetDecimal("price"),
            Quantity    = item.GetN<long>("quantity"),
        };
    }
}
