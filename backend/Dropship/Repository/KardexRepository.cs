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

        return items.Select(KardexMapper.ToDomain).ToList();
    }
}