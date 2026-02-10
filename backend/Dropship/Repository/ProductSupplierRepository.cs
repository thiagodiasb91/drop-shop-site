using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para gerenciar relações entre Produtos e Fornecedores
/// Permite buscar produtos por fornecedor de forma eficiente
/// </summary>
public class ProductSupplierRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<ProductSupplierRepository> _logger;

    public ProductSupplierRepository(DynamoDbRepository repository, ILogger<ProductSupplierRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Cria um registro de relação Product-Supplier
    /// </summary>
    public async Task<ProductSupplierDomain> CreateProductSupplierAsync(
        string productId,
        string supplierId,
        string productName,
        decimal productionPrice,
        int skuCount,
        int priority = 0)
    {
        _logger.LogInformation("Creating product-supplier link - ProductId: {ProductId}, SupplierId: {SupplierId}, SKUCount: {SKUCount}",
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
                ProductionPrice = productionPrice,
                SkuCount = skuCount,
                Priority = priority,
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
                { "production_price", new AttributeValue { N = record.ProductionPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { "sku_count", new AttributeValue { N = record.SkuCount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { "priority", new AttributeValue { N = record.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { "created_at", new AttributeValue { S = createdAtUtc } }
            };

            await _repository.PutItemAsync(item);

            _logger.LogInformation("Product-supplier link created - ProductId: {ProductId}, SupplierId: {SupplierId}",
                productId, supplierId);

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product-supplier link - ProductId: {ProductId}, SupplierId: {SupplierId}",
                productId, supplierId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os produtos fornecidos por um fornecedor específico
    /// </summary>
    public async Task<List<ProductSupplierDomain>> GetProductsBySupplierAsync(string supplierId)
    {
        _logger.LogInformation("Getting products for supplier - SupplierId: {SupplierId}", supplierId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { ":sk_prefix", new AttributeValue { S = "Product#" } }
            };

            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_prefix)",
                expressionAttributeValues: expressionAttributeValues
            );

            if (items == null || items.Count == 0)
            {
                _logger.LogDebug("No products found for supplier - SupplierId: {SupplierId}", supplierId);
                return new List<ProductSupplierDomain>();
            }

            var products = items
                .Select(ProductSupplierMapper.ToDomain)
                .OrderBy(p => p.Priority)
                .ToList();

            _logger.LogInformation("Found {Count} products for supplier - SupplierId: {SupplierId}",
                products.Count, supplierId);

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for supplier - SupplierId: {SupplierId}", supplierId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um registro específico de Product-Supplier
    /// </summary>
    public async Task<ProductSupplierDomain?> GetProductSupplierAsync(string supplierId, string productId)
    {
        _logger.LogInformation("Getting product-supplier record - SupplierId: {SupplierId}, ProductId: {ProductId}",
            supplierId, productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            var item = await _repository.GetItemAsync(key);
            return item != null ? ProductSupplierMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product-supplier record - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza um registro de Product-Supplier
    /// </summary>
    public async Task<ProductSupplierDomain?> UpdateProductSupplierAsync(
        string supplierId,
        string productId,
        decimal productionPrice,
        int skuCount)
    {
        _logger.LogInformation("Updating product-supplier record - SupplierId: {SupplierId}, ProductId: {ProductId}",
            supplierId, productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            var updateExpression = "SET production_price = :price, sku_count = :count";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":price", new AttributeValue { N = productionPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { ":count", new AttributeValue { N = skuCount.ToString(System.Globalization.CultureInfo.InvariantCulture) } }
            };

            await _repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            var item = await _repository.GetItemAsync(key);
            return item != null ? ProductSupplierMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product-supplier record - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);
            throw;
        }
    }

    /// <summary>
    /// Remove um produto da lista de produtos fornecidos por um fornecedor
    /// </summary>
    public async Task<bool> RemoveProductSupplierAsync(string supplierId, string productId)
    {
        _logger.LogInformation("Removing product from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
            supplierId, productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            await _repository.DeleteItemAsync(key);

            _logger.LogInformation("Product removed from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);
            throw;
        }
    }
}

