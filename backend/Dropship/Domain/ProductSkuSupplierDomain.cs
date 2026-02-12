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
    public static ProductSkuSupplierDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        return new ProductSkuSupplierDomain
        {
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",

            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "product_sku_supplier",
            ProductId = item.ContainsKey("product_id") ? item["product_id"].S : "",
            Sku = item.ContainsKey("sku") ? item["sku"].S : "",
            SupplierId = item.ContainsKey("supplier_id") ? item["supplier_id"].S : "",
            SkuSupplier = item.ContainsKey("sku_supplier") ? item["sku_supplier"].S : "",
            
            Price = item.ContainsKey("price") && decimal.TryParse(item["price"].N, System.Globalization.CultureInfo.InvariantCulture, out var price) ? price : 0,
            Quantity = item.ContainsKey("quantity") && long.TryParse(item["quantity"].N, out var qty) ? qty : 0
        };
    }

    public static List<ProductSkuSupplierDomain> ToDomainList(this List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}

