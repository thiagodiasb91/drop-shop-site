using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Dropship.Repository;

public class DynamoDbRepository(IAmazonDynamoDB client)
{
    private const string TableName = "catalog-core";

    public async Task<Dictionary<string, AttributeValue>?> GetItemAsync(Dictionary<string, AttributeValue> key)
    {
        var response = await client.GetItemAsync(TableName, key);
        return response != null && response.Item != null && response.Item.Any() ? response.Item : null;
    }

    public async Task<PutItemResponse> PutItemAsync(Dictionary<string, AttributeValue> item)
    {
        return await client.PutItemAsync(TableName, item);
    }

    public async Task<UpdateItemResponse> UpdateItemAsync(Dictionary<string, AttributeValue> key, string updateExpression, Dictionary<string, AttributeValue> expressionAttributeValues)
    {
        return await client.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = TableName,
            Key = key,
            UpdateExpression = updateExpression,
            ExpressionAttributeValues = expressionAttributeValues,
            ReturnValues = ReturnValue.UPDATED_NEW
        });
    }

    public async Task<DeleteItemResponse> DeleteItemAsync(Dictionary<string, AttributeValue> key)
    {
        return await client.DeleteItemAsync(TableName, key);
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
            TableName = TableName,
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