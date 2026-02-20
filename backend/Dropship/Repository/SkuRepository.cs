using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Dropship.Requests;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com SKU no DynamoDB
/// </summary>
public class SkuRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<SkuRepository> _logger;

    public SkuRepository(DynamoDbRepository repository, ILogger<SkuRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtém um SKU pelo ID do produto e código SKU
    /// </summary>
    public async Task<SkuDomain?> GetSkuAsync(string productId, string sku)
    {
        _logger.LogInformation("Getting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}" } }
            };

            var item = await _repository.GetItemAsync(key);
            if (item == null)
            {
                _logger.LogWarning("SKU not found - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return null;
            }

            var skuDomain = SkuMapper.ToDomain(item);
            _logger.LogInformation("SKU found - ProductId: {ProductId}, SKU: {Sku}, Quantity: {Quantity}", 
                productId, sku, skuDomain.Quantity);
            return skuDomain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            throw;
        }
    }

    /// <summary>
    /// Cria um novo SKU para um produto
    /// </summary>
    public async Task<SkuDomain> CreateSkuAsync(CreateSkuRequest request)
    {
        _logger.LogInformation("Creating SKU - ProductId: {ProductId}, SKU: {Sku}, Size: {Size}, Color: {Color}, Quantity: {Quantity}",
            request.ProductId, request.Sku, request.Size, request.Color, request.Quantity);

        try
        {
            var createdAtUtc = DateTime.UtcNow.ToString("O");

            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{request.ProductId}" } },
                { "SK", new AttributeValue { S = $"Sku#{request.Sku}" } },
                { "productId", new AttributeValue { S = request.ProductId } },
                { "sku", new AttributeValue { S = request.Sku } },
                { "size", new AttributeValue { S = request.Size } },
                { "color", new AttributeValue { S = request.Color } },
                { "quantity", new AttributeValue { N = request.Quantity.ToString() } },
                { "entityType", new AttributeValue { S = "sku" } },
                { "created_at", new AttributeValue { S = createdAtUtc } }
            };

            await _repository.PutItemAsync(item);

            _logger.LogInformation("SKU created successfully - ProductId: {ProductId}, SKU: {Sku}",
                request.ProductId, request.Sku);

            return SkuMapper.ToDomain(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SKU - ProductId: {ProductId}, SKU: {Sku}",
                request.ProductId, request.Sku);
            throw;
        }
    }

    /// <summary>
    /// Atualiza um SKU existente
    /// </summary>
    public async Task<SkuDomain?> UpdateSkuAsync(string productId, string sku, UpdateSkuRequest request)
    {
        _logger.LogInformation("Updating SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            var existingSku = await GetSkuAsync(productId, sku);
            if (existingSku == null)
            {
                _logger.LogWarning("SKU not found for update - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return null;
            }

            // Atualizar apenas campos fornecidos
            var updatedSize = request.Size ?? existingSku.Size;
            var updatedColor = request.Color ?? existingSku.Color;
            var updatedQuantity = request.Quantity ?? existingSku.Quantity;

            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = existingSku.PK } },
                { "SK", new AttributeValue { S = existingSku.SK } },
                { "productId", new AttributeValue { S = productId } },
                { "sku", new AttributeValue { S = sku } },
                { "size", new AttributeValue { S = updatedSize } },
                { "color", new AttributeValue { S = updatedColor } },
                { "quantity", new AttributeValue { N = updatedQuantity.ToString() } },
                { "entityType", new AttributeValue { S = existingSku.EntityType } },
                { "created_at", new AttributeValue { S = existingSku.CreatedAt.ToString("O") } },
                { "updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await _repository.PutItemAsync(item);

            _logger.LogInformation("SKU updated successfully - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

            return SkuMapper.ToDomain(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            throw;
        }
    }

    /// <summary>
    /// Deleta um SKU
    /// </summary>
    public async Task<bool> DeleteSkuAsync(string productId, string sku)
    {
        _logger.LogInformation("Deleting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}" } }
            };

            var response = await _repository.DeleteItemAsync(key);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("SKU deleted successfully - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return true;
            }

            _logger.LogWarning("Failed to delete SKU - ProductId: {ProductId}, SKU: {Sku}, StatusCode: {StatusCode}",
                productId, sku, response.HttpStatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            throw;
        }
    }

    /// <summary>
    /// Lista todos os SKUs de um produto
    /// Usa query no DynamoDB com PK = "Product#{productId}" e begins_with SK = "Sku#"
    /// </summary>
    public async Task<List<SkuDomain>> GetSkusByProductIdAsync(string productId)
    {
        _logger.LogInformation("Fetching all SKUs for product - ProductId: {ProductId}", productId);

        try
        {
            var items = await _repository.QueryTableAsync(
                indexName: null,
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Product#{productId}" } },
                    { ":sk", new AttributeValue { S = "Sku#" } }
                }
            );

            if (items.Count == 0)
            {
                _logger.LogInformation("No SKUs found for product - ProductId: {ProductId}", productId);
                return new List<SkuDomain>();
            }

            var skus = items
                .Select(SkuMapper.ToDomain)
                .Where( x=> x.EntityType == "sku") ///TODO: REMOVER quando FOR PARA SQL
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} SKUs for product - ProductId: {ProductId}", 
                skus.Count, productId);
            return skus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching SKUs for product - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Lista todos os SKUs no sistema
    /// Usa query no DynamoDB com GSI_RELATIONS_LOOKUP
    /// </summary>
    public async Task<List<SkuDomain>> GetAllSkusAsync()
    {
        _logger.LogInformation("Fetching all SKUs");

        try
        {
            var items = await _repository.QueryTableAsync(
                indexName: "GSI_RELATIONS_LOOKUP",
                keyConditionExpression: "begins_with(PK, :pk) AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = "Product#" } },
                    { ":sk", new AttributeValue { S = "Sku#" } }
                }
            );

            if (items.Count == 0)
            {
                _logger.LogInformation("No SKUs found");
                return new List<SkuDomain>();
            }

            var skus = items
                .Select(SkuMapper.ToDomain)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} SKUs", skus.Count);
            return skus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all SKUs");
            throw;
        }
    }

    /// <summary>
    /// Atualiza a quantidade de um SKU
    /// </summary>
    public async Task<SkuDomain?> UpdateSkuQuantityAsync(string productId, string sku, int quantity)
    {
        _logger.LogInformation("Updating SKU quantity - ProductId: {ProductId}, SKU: {Sku}, Quantity: {Quantity}",
            productId, sku, quantity);

        try
        {
            var updateRequest = new UpdateSkuRequest { Quantity = quantity };
            return await UpdateSkuAsync(productId, sku, updateRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SKU quantity - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            throw;
        }
    }
}
