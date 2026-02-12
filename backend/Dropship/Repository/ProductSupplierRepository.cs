using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

public class ProductSupplierRepository(DynamoDbRepository repository,
                                      ProductRepository productRepository,
    ILogger<ProductSupplierRepository> logger)
{
    
    public async Task CreateProductSupplierAsync(
        string productId,
        string supplierId,
        string productName,
        int skuCount)
    {
        logger.LogInformation("Creating product-supplier link - ProductId: {ProductId}, SupplierId: {SupplierId}, SKUCount: {SKUCount}",
            productId, supplierId, skuCount);

        try
        {
            var createdAtUtc = DateTime.UtcNow.ToString("O"); // ISO 8601 format

            var record = new ProductSupplierDomain
            {
                Pk = $"Supplier#{supplierId}",
                Sk = $"Product#{productId}",
                EntityType = "product_supplier",
                ProductId = productId,
                ProductName = productName,
                SupplierId = supplierId,
                SkuCount = skuCount,
                CreatedAt = DateTime.UtcNow
            };

            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = record.Pk } },
                { "SK", new AttributeValue { S = record.Sk } },
                { "entity_type", new AttributeValue { S = record.EntityType } },
                { "product_id", new AttributeValue { S = record.ProductId } },
                { "product_name", new AttributeValue { S = record.ProductName } },
                { "supplier_id", new AttributeValue { S = record.SupplierId } },
                { "sku_count", new AttributeValue { N = record.SkuCount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { "created_at", new AttributeValue { S = createdAtUtc } }
            };

            await repository.PutItemAsync(item);

            logger.LogInformation("Product-supplier link created - ProductId: {ProductId}, SupplierId: {SupplierId}", productId, supplierId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product-supplier link - ProductId: {ProductId}, SupplierId: {SupplierId}", productId, supplierId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os produtos fornecidos por um fornecedor específico
    /// </summary>
    public async Task<List<ProductSupplierDomain>> GetProductsBySupplierAsync(string supplierId)
    {
        logger.LogInformation("Getting products for supplier - SupplierId: {SupplierId}", supplierId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { ":sk_prefix", new AttributeValue { S = "Product#" } }
            };

            var items = await repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_prefix)",
                expressionAttributeValues: expressionAttributeValues
            );

            if (items == null || items.Count == 0)
            {
                logger.LogDebug("No products found for supplier - SupplierId: {SupplierId}", supplierId);
                return new List<ProductSupplierDomain>();
            }

            var products = items
                .Select(ProductSupplierMapper.ToDomain)
                .ToList();

            logger.LogInformation("Found {Count} products for supplier - SupplierId: {SupplierId}",
                products.Count, supplierId);

            return products;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products for supplier - SupplierId: {SupplierId}", supplierId);
            throw;
        }
    }

    /// <summary>
    /// Remove um produto da lista de produtos fornecidos por um fornecedor
    /// </summary>
    public async Task RemoveProductSupplierAsync(string supplierId, string productId)
    {
        logger.LogInformation("Removing product from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
            supplierId, productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            await repository.DeleteItemAsync(key);

            logger.LogInformation("Product removed from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing product from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);
            throw;
        }
    }

    public async Task<bool> HasSupplierRelation(string productId)
    {
        logger.LogInformation("Checking if product has supplier relation - ProductId: {ProductId}", productId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = "Supplier#" } },
                { ":sk", new AttributeValue { S = $"Product#{productId}" } }
            };

            var items = await repository.QueryTableAsync(
                indexName: "GSI_RELATIONS",
                keyConditionExpression: "SK = :sk AND begins_with(PK, :pk)",
                expressionAttributeValues: expressionAttributeValues
            );

            return items != null && items.Count > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing product from supplier - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os produtos que possuem pelo menos um fornecedor vinculado
    /// Usa foreach assíncrono para verificar relações em paralelo quando possível
    /// </summary>
    /// <returns>Lista de produtos que têm fornecedores vinculados</returns>
    public async Task<List<ProductDomain>> GetAllProductsWithSupplier()
    {
        logger.LogInformation("Getting all products with supplier relations");

        try
        {
            var products = await productRepository.GetAllProductsAsync();
            var productsWithSupplier = new List<ProductDomain>();

            foreach (var product in products)
            {
                if (!await HasSupplierRelation(product.Id)) continue;
                
                productsWithSupplier.Add(product);
                logger.LogDebug("Product has supplier relation - ProductId: {ProductId}", product.Id);
            }

            logger.LogInformation("Found {Count} products with supplier relations out of {Total} total products",
                productsWithSupplier.Count, products.Count);

            return productsWithSupplier;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all products with supplier relations");
            throw;
        }
    }
}

