namespace Dropship.Domain;

public class KardexDomain
{
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public string? OrderSn { get; set; }
    public long? ShopId { get; set; }
}

/// <summary>
/// Mapper para converter DynamoDB items para KardexDomain
/// Centraliza a lógica de mapeamento seguindo o padrão do projeto
/// </summary>
public static class KardexMapper
{
    public static KardexDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        return new KardexDomain
        {
            PK = item.ContainsKey("PK") ? item["PK"].S : string.Empty,
            SK = item.ContainsKey("SK") ? item["SK"].S : string.Empty,
            Date = item.ContainsKey("date") ? item["date"].S : string.Empty,
            EntityType = item.ContainsKey("entityType") ? item["entityType"].S : string.Empty,
            Operation = item.ContainsKey("operation") ? item["operation"].S : string.Empty,
            ProductId = item.ContainsKey("product_id") ? item["product_id"].S : string.Empty,
            Quantity = item.ContainsKey("quantity") && int.TryParse(item["quantity"].N, out var qty) ? qty : 0,
            SupplierId = item.ContainsKey("supplier_id") ? item["supplier_id"].S : string.Empty,
            OrderSn = item.ContainsKey("ordersn") ? item["ordersn"].S : null,
            ShopId = item.ContainsKey("shop_id") && long.TryParse(item["shop_id"].N, out var shopId) ? shopId : null
        };
    }

    public static List<KardexDomain> ToDomainList(this List<Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>> items)
    {
        return items.Select(ToDomain).ToList();
    }
}
