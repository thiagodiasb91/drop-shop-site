using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

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
    public static KardexDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new KardexDomain
        {
            PK = item.GetS("PK"),
            SK = item.GetS("SK"),
            Date = item.GetS("date"),
            EntityType = item.GetS("entity_type"),
            Operation = item.GetS("operation"),
            ProductId = item.GetS("product_id"),
            Quantity = item.GetN<int>("quantity"),
            SupplierId = item.GetS("supplier_id"),
            OrderSn = item.GetSNullable("ordersn"),
            ShopId = item.GetUnixTimestampNullable("shop_id"),
        };
    }

    public static List<KardexDomain> ToDomainList(this List<Dictionary<string, AttributeValue>> items)
        => items.Select(ToDomain).ToList();
}
