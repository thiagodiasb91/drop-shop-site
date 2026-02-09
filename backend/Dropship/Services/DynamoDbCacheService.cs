using Amazon.DynamoDBv2.Model;
using Dropship.Repository;

namespace Dropship.Services;

/// <summary>
/// Implementação do cache usando DynamoDB
/// Estrutura:
/// - PK: Cache#{key}
/// - SK: META
/// - value: valor armazenado
/// - entityType: cache
/// </summary>
public class DynamoDbCacheService : ICacheService
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<DynamoDbCacheService> _logger;
    
    private const string EntityType = "cache";
    private const string SortKey = "META";

    public DynamoDbCacheService(DynamoDbRepository repository, ILogger<DynamoDbCacheService> logger)
    {
        _repository = repository;
        _logger = logger;
        _logger.LogInformation("DynamoDbCacheService initialized");
    }

    private static string GetPartitionKey(string key) => $"Cache#{key}";

    private Dictionary<string, AttributeValue> GetKeyAttributes(string key)
    {
        return new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = GetPartitionKey(key) } },
            { "SK", new AttributeValue { S = SortKey } }
        };
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            _logger.LogDebug("DynamoDbCache Get - Key: {Key}", key);
            
            var keyAttributes = GetKeyAttributes(key);
            var item = await _repository.GetItemAsync(keyAttributes);
            
            if (item == null || !item.ContainsKey("value"))
            {
                _logger.LogDebug("DynamoDbCache Get - Key: {Key} not found", key);
                return null;
            }
            
            var value = item["value"].S;
            _logger.LogDebug("DynamoDbCache Get - Key: {Key}, Found: true", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from DynamoDB cache - Key: {Key}", key);
            return null;
        }
    }

    public async Task<Dictionary<string, string?>> GetManyAsync(params string[] keys)
    {
        var result = new Dictionary<string, string?>();
        
        try
        {
            _logger.LogDebug("DynamoDbCache GetMany - Keys: {Keys}", string.Join(", ", keys));
            
            // DynamoDB não tem BatchGetItem no repository base, então fazemos queries individuais
            // Para melhor performance, podemos paralelizar as chamadas
            var tasks = keys.Select(async key =>
            {
                var value = await GetAsync(key);
                return (key, value);
            });
            
            var results = await Task.WhenAll(tasks);
            
            foreach (var (key, value) in results)
            {
                result[key] = value;
            }
            
            _logger.LogDebug("DynamoDbCache GetMany Result - Found {Count} items", result.Count(kv => kv.Value != null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting values from DynamoDB cache");
            // Retorna dicionário com nulls para as keys solicitadas
            foreach (var key in keys)
            {
                if (!result.ContainsKey(key))
                {
                    result[key] = null;
                }
            }
        }
        
        return result;
    }

    public async Task<bool> SaveAsync(string key, string? value)
    {
        try
        {
            _logger.LogDebug("DynamoDbCache Save - Key: {Key}", key);
            
            if (value == null)
            {
                return await DeleteAsync(key);
            }
            
            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = GetPartitionKey(key) } },
                { "SK", new AttributeValue { S = SortKey } },
                { "entity_type", new AttributeValue { S = EntityType } },
                { "cacheKey", new AttributeValue { S = key } },
                { "value", new AttributeValue { S = value } },
                { "updatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };
            
            await _repository.PutItemAsync(item);
            _logger.LogDebug("DynamoDbCache Saved - Key: {Key}", key);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving value to DynamoDB cache - Key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> SaveManyAsync(params (string key, string? value)[] keyValues)
    {
        try
        {
            _logger.LogDebug("DynamoDbCache SaveMany - Items: {Count}", keyValues.Length);
            
            // Paraleliza as operações de save
            var tasks = keyValues.Select(kv => SaveAsync(kv.key, kv.value));
            var results = await Task.WhenAll(tasks);
            
            var success = results.All(r => r);
            _logger.LogDebug("DynamoDbCache SaveMany - Success: {Success}, Items: {Count}", success, keyValues.Length);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving values to DynamoDB cache");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            _logger.LogDebug("DynamoDbCache Delete - Key: {Key}", key);
            
            var keyAttributes = GetKeyAttributes(key);
            await _repository.DeleteItemAsync(keyAttributes);
            
            _logger.LogDebug("DynamoDbCache Deleted - Key: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key from DynamoDB cache - Key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var value = await GetAsync(key);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists in DynamoDB cache - Key: {Key}", key);
            return false;
        }
    }
}

