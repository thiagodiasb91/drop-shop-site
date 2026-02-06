using Amazon.DynamoDBv2.Model;
using Dropship.Repository;
using Dropship.Domain;

namespace Dropship.Services;

public class KardexService
{
    private readonly DynamoDbRepository _repository;

    public KardexService(DynamoDbRepository repository)
    {
        _repository = repository;
    }

    public async Task AddToKardexAsync(KardexDomain kardex)
    {
        var sortedId = Ulid.NewUlid().ToString();
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"Kardex#Sku#{kardex.SK}" } },
            { "SK", new AttributeValue { S = sortedId } },
            { "product_id", new AttributeValue { S = kardex.ProductId } },
            { "entityType", new AttributeValue { S = "kardex" } },
            { "quantity", new AttributeValue { N = kardex.Quantity.ToString() } },
            { "operation", new AttributeValue { S = kardex.Operation } },
            { "date", new AttributeValue { S = DateTime.UtcNow.ToString("O") } },
            { "supplier_id", new AttributeValue { S = kardex.SupplierId } }
        };

        await _repository.PutItemAsync(item);
    }
}