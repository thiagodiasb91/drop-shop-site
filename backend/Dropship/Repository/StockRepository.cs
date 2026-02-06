using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

public class StockRepository
{
    private readonly DynamoDbRepository _repository;

    public StockRepository(DynamoDbRepository repository)
    {
        _repository = repository;
    }

    public async Task<(bool exists, string productId)> VerifySkuExistsAsync(string sku)
    {
        var items = await _repository.QueryTableAsync(
            "SK = :sk",
            expressionAttributeValues: new Dictionary<string, AttributeValue>
            {
                { ":sk", new AttributeValue { S = $"Sku#{sku}" } }
            },
            indexName: "GSI_RELATIONS"
        );

        if (!items.Any())
            return (false, string.Empty);

        var productId = items[0]["PK"].S.Replace("Product#", "");
        return (true, productId);
    }

    public async Task UpdateProductStockAsync(string supplierId, string productId, string sku, int quantity)
    {
        var key = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"Product#{productId}" } },
            { "SK", new AttributeValue { S = $"Sku#{sku}#Supplier#{supplierId}" } }
        };

        await _repository.UpdateItemAsync(
            key,
            "SET quantity = :q",
            new Dictionary<string, AttributeValue>
            {
                { ":q", new AttributeValue { N = quantity.ToString() } }
            }
        );
    }
}