using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Dropship.Requests;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com Product no DynamoDB
/// </summary>
public class ProductRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(DynamoDbRepository repository, ILogger<ProductRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtém um produto pelo ID
    /// </summary>
    public async Task<ProductDomain?> GetProductByIdAsync(string productId)
    {
        _logger.LogInformation("Getting product by ID - ProductId: {ProductId}", productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = "META" } }
            };

            var item = await _repository.GetItemAsync(key);
            if (item == null)
            {
                _logger.LogWarning("Product not found - ProductId: {ProductId}", productId);
                return null;
            }

            var product = ProductMapper.ToDomain(item);
            _logger.LogInformation("Product found - ProductId: {ProductId}, Name: {ProductName}", productId, product.Name);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Cria um novo produto
    /// </summary>
    public async Task<ProductDomain> CreateProductAsync(CreateProductRequest request)
    {
        _logger.LogInformation("Creating product - ProductName: {ProductName}", request.ProductName);

        try
        {
            var productId = Guid.NewGuid().ToString();
            var createdAtUtc = DateTime.UtcNow.ToString("O"); // ISO 8601 format

            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = "META" } },
                { "product_id", new AttributeValue { S = productId } },
                { "entityType", new AttributeValue { S = "product" } },
                { "product_name", new AttributeValue { S = request.ProductName } },
                { "created_at", new AttributeValue { S = createdAtUtc } }
            };

            await _repository.PutItemAsync(item);

            _logger.LogInformation("Product created successfully - ProductId: {ProductId}, ProductName: {ProductName}", 
                productId, request.ProductName);

            return ProductMapper.ToDomain(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product - ProductName: {ProductName}", request.ProductName);
            throw;
        }
    }

    /// <summary>
    /// Atualiza um produto existente
    /// </summary>
    public async Task<ProductDomain?> UpdateProductAsync(string productId, UpdateProductRequest request)
    {
        _logger.LogInformation("Updating product - ProductId: {ProductId}", productId);

        try
        {
            var existingProduct = await GetProductByIdAsync(productId);
            if (existingProduct == null)
            {
                _logger.LogWarning("Product not found for update - ProductId: {ProductId}", productId);
                return null;
            }

            // Atualizar apenas campos fornecidos
            var updatedName = request.ProductName ?? existingProduct.Name;

            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = existingProduct.PK } },
                { "SK", new AttributeValue { S = existingProduct.SK } },
                { "product_id", new AttributeValue { S = productId } },
                { "entityType", new AttributeValue { S = existingProduct.EntityType } },
                { "product_name", new AttributeValue { S = updatedName } },
                { "created_at", new AttributeValue { S = existingProduct.CreatedAt.ToString("O") } },
                { "updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await _repository.PutItemAsync(item);

            _logger.LogInformation("Product updated successfully - ProductId: {ProductId}", productId);

            return ProductMapper.ToDomain(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Deleta um produto
    /// </summary>
    public async Task<bool> DeleteProductAsync(string productId)
    {
        _logger.LogInformation("Deleting product - ProductId: {ProductId}", productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = "META" } }
            };

            var response = await _repository.DeleteItemAsync(key);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Product deleted successfully - ProductId: {ProductId}", productId);
                return true;
            }

            _logger.LogWarning("Failed to delete product - ProductId: {ProductId}, StatusCode: {StatusCode}", 
                productId, response.HttpStatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Lista todos os produtos
    /// Usa query no DynamoDB com begins_with para PK = "Product#"
    /// </summary>
    public async Task<List<ProductDomain>> GetAllProductsAsync()
    {
        _logger.LogInformation("Fetching all products");

        try
        {
            var items = await _repository.QueryTableAsync(
                indexName: "GSI_RELATIONS_LOOKUP",
                keyConditionExpression: "SK = :sk AND begins_with(PK, :pk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":sk", new AttributeValue { S = "META" } },
                    { ":pk", new AttributeValue { S = "Product#" } }
                }
            );

            if (items.Count == 0)
            {
                _logger.LogInformation("No products found");
                return new List<ProductDomain>();
            }

            var products = items
                .Select(ProductMapper.ToDomain)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} products", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all products");
            throw;
        }
    }
}
