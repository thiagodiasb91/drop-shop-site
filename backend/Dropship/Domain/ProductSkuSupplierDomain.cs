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

    // Identificadores
    public string EntityType { get; set; } = "product_sku_supplier";
    public string ProductId { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string SupplierId { get; set; } = default!;

    // Dados do Fornecimento
    public decimal ProductionPrice { get; set; } // Preço de produção do fornecedor
    public long Quantity { get; set; } // Quantidade disponível
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
            // Chaves
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",

            // Identificadores
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "product_sku_supplier",
            ProductId = item.ContainsKey("product_id") ? item["product_id"].S : "",
            Sku = item.ContainsKey("sku") ? item["sku"].S : "",
            SupplierId = item.ContainsKey("supplier_id") ? item["supplier_id"].S : "",

            // Dados
            ProductionPrice = item.ContainsKey("production_price") && decimal.TryParse(item["production_price"].N, System.Globalization.CultureInfo.InvariantCulture, out var price) ? price : 0,
            Quantity = item.ContainsKey("quantity") && long.TryParse(item["quantity"].N, out var qty) ? qty : 0
        };
    }

    public static List<ProductSkuSupplierDomain> ToDomainList(this List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}

