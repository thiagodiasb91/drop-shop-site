using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

/// <summary>
/// Domínio para representar um vendedor (Seller)
/// PK = Seller#{SellerId} | SK = META
/// </summary>
public class SellerDomain
{
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string EntityType { get; set; } = "seller";
    public string Marketplace { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public long ShopId { get; set; }
    public string? InfinityPayHandle { get; set; }
    public long? CreatedAt { get; set; }
    public long? UpdatedAt { get; set; }
}

/// <summary>
/// Mapper para converter entre SellerDomain e DynamoDB
/// </summary>
public static class SellerMapper
{
    public static SellerDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new SellerDomain
        {
            PK                = item.GetS("PK"),
            SK                = item.GetS("SK"),
            EntityType        = item.GetS("entity_type", "seller"),
            Marketplace       = item.GetS("marketplace"),
            SellerId          = item.GetS("seller_id"),
            SellerName        = item.GetS("seller_name"),
            ShopId            = item.GetN<long>("shop_id"),
            InfinityPayHandle = item.GetSNullable("infinity_pay_handle"),
            CreatedAt         = item.GetUnixTimestampNullable("createdAt"),
            UpdatedAt         = item.GetUnixTimestampNullable("updatedAt"),
        };
    }

    public static Dictionary<string, AttributeValue> ToDynamoDb(this SellerDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK",           new AttributeValue { S = domain.PK } },
            { "SK",           new AttributeValue { S = domain.SK } },
            { "entity_type",  new AttributeValue { S = domain.EntityType } },
            { "marketplace",  new AttributeValue { S = domain.Marketplace } },
            { "seller_id",     new AttributeValue { S = domain.SellerId } },
            { "seller_name",   new AttributeValue { S = domain.SellerName } },
            { "shop_id",      new AttributeValue { N = domain.ShopId.ToString() } },
            { "createdAt",    new AttributeValue { N = (domain.CreatedAt ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString() } },
            { "updatedAt",    new AttributeValue { N = (domain.UpdatedAt ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString() } },
        };

        if (!string.IsNullOrWhiteSpace(domain.InfinityPayHandle))
            item["infinity_pay_handle"] = new AttributeValue { S = domain.InfinityPayHandle };

        return item;
    }
}
