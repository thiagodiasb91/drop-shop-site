using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Dropship.Requests;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com Product no DynamoDB
/// </summary>
public class ProductRepository(DynamoDbRepository repository, ILogger<ProductRepository> logger)
{
    /// <summary>
    /// Obtém um produto pelo ID
    /// </summary>
    public async Task<ProductDomain?> GetProductByIdAsync(string productId)
    {
        logger.LogInformation("Getting product by ID - ProductId: {ProductId}", productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = "Product" } },
                { "SK", new AttributeValue { S = $"META#{productId}" } }
            };

            var item = await repository.GetItemAsync(key);
            if (item == null)
            {
                logger.LogWarning("Product not found - ProductId: {ProductId}", productId);
                return null;
            }

            var product = ProductMapper.ToDomain(item);
            logger.LogInformation("Product found - ProductId: {ProductId}, Name: {ProductName}", productId,
                product.Name);
            return product;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting product - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Lista todos os produtos
    /// Usa query no DynamoDB com begins_with para PK = "Product#"
    /// </summary>
    public async Task<List<ProductDomain>> GetAllProductsAsync()
    {
        logger.LogInformation("Fetching all products");

        try
        {
            var items = await repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":sk", new AttributeValue { S = "META#" } },
                    { ":pk", new AttributeValue { S = "Product" } }
                }
            );

            if (items.Count == 0)
            {
                logger.LogInformation("No products found");
                return new List<ProductDomain>();
            }

            var products = items
                .Select(ProductMapper.ToDomain)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            logger.LogInformation("Retrieved {Count} products", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching all products");
            throw;
        }
    }

    /// <summary>
    /// Obtém todas as imagens de um produto pelo ProductId
    /// Busca por PK = "Product#{productId}" e SK começando com "Color#"
    /// Filtra apenas registros com entity_type = "product_image"
    /// </summary>
    public async Task<List<ProductImageDomain>> GetImagesByProductIdAsync(string productId)
    {
        logger.LogInformation("Getting images for product - ProductId: {ProductId}", productId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Product#{productId}" } },
                { ":sk_prefix", new AttributeValue { S = "Color#" } },
                { ":entity_type", new AttributeValue { S = "product_image" } }
            };

            var items = await repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_prefix)",
                filterExpression: "entity_type = :entity_type",
                expressionAttributeValues: expressionAttributeValues
            );

            if (items == null || items.Count == 0)
            {
                logger.LogDebug("No images found for product - ProductId: {ProductId}", productId);
                return new List<ProductImageDomain>();
            }

            var images = items
                .Select(ProductImageMapper.ToDomain)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            logger.LogInformation("Retrieved {Count} images for product - ProductId: {ProductId}", images.Count,
                productId);
            return images;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting images for product - ProductId: {ProductId}", productId);
            throw;
        }
    }
}
