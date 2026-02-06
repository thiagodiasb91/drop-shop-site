using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

public class KardexRepository
{
    private readonly DynamoDbRepository _repository;

    public KardexRepository(DynamoDbRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<KardexDomain>> GetKardexBySkuAsync(string sku)
    {
        var keyConditionExpression = "PK = :pk";
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":pk", new AttributeValue { S = $"Kardex#Sku#{sku}" } }
        };

        var items = await _repository.QueryTableAsync(
            keyConditionExpression,
            expressionAttributeValues: expressionAttributeValues
        );

        return items.Select(MapToKardexDomain).ToList();
    }

    private KardexDomain MapToKardexDomain(Dictionary<string, AttributeValue> item)
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
}