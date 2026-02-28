using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Dropship.Repository;

public class DynamoDbRepository(IAmazonDynamoDB client)
{
    private string _tableName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development" ? "catalog-core" : "catalog-core-prd";

    public async Task<Dictionary<string, AttributeValue>?> GetItemAsync(Dictionary<string, AttributeValue> key)
    {
        var response = await client.GetItemAsync(_tableName, key);
        return response != null && response.Item != null && response.Item.Any() ? response.Item : null;
    }

    public async Task PutItemAsync(Dictionary<string, AttributeValue> item)
    {
        await client.PutItemAsync(_tableName, item);
    }

    public async Task UpdateItemAsync(Dictionary<string, AttributeValue> key, string updateExpression,
        Dictionary<string, AttributeValue> expressionAttributeValues)
    {
        await client.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _tableName,
            Key = key,
            UpdateExpression = updateExpression,
            ExpressionAttributeValues = expressionAttributeValues,
            ReturnValues = ReturnValue.UPDATED_NEW
        });
    }

    public async Task<DeleteItemResponse> DeleteItemAsync(Dictionary<string, AttributeValue> key)
    {
        return await client.DeleteItemAsync(_tableName, key);
    }

    public async Task<List<Dictionary<string, AttributeValue>>> QueryTableAsync(
        string keyConditionExpression,
        Dictionary<string, string>? expressionAttributeNames = null,
        Dictionary<string, AttributeValue>? expressionAttributeValues = null,
        string? filterExpression = null,
        string? indexName = null)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = keyConditionExpression
        };

        if (expressionAttributeNames != null)
            request.ExpressionAttributeNames = expressionAttributeNames;
        
        if (expressionAttributeValues != null)
            request.ExpressionAttributeValues = expressionAttributeValues;
        
        if (filterExpression != null)
            request.FilterExpression = filterExpression;
        
        if (indexName != null)
            request.IndexName = indexName;

        var response = await client.QueryAsync(request);
        return response.Items;
    }
}