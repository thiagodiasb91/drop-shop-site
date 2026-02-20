using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para gerenciar relações entre Produtos e Vendedores (Marketplace)
/// Permite buscar produtos por vendedor de forma eficiente
/// </summary>
public class ProductSellerRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<ProductSellerRepository> _logger;

    public ProductSellerRepository(DynamoDbRepository repository, ILogger<ProductSellerRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Cria um registro META de relação Product-Seller
    /// </summary>
    public async Task<ProductSellerDomain> CreateProductSellerAsync(
        string productId,
        string productName,
        string sellerId,
        string marketplace,
        long storeId,
        decimal price,
        string supplierId,
        int skuCount)
    {
        _logger.LogInformation("Creating product-seller META - ProductId: {ProductId}, SellerId: {SellerId}, Marketplace: {Marketplace}",
            productId, sellerId, marketplace);

        try
        {
            var createdAtUtc = DateTime.UtcNow.ToString("O");

            var record = new ProductSellerDomain
            {
                Pk = $"Seller#{marketplace}#{sellerId}",
                Sk = $"Product#{productId}#Supplier#{supplierId}",
                EntityType = "product_seller",
                ProductId = productId,
                ProductName = productName,
                SellerId = sellerId,
                Marketplace = marketplace,
                StoreId = storeId,
                SkuCount = skuCount,
                SupplierId = supplierId,
                Price = price,
                CreatedAt = DateTime.UtcNow
            };

            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = record.Pk } },
                { "SK", new AttributeValue { S = record.Sk } },
                { "entity_type", new AttributeValue { S = record.EntityType } },
                { "product_id", new AttributeValue { S = record.ProductId } },
                { "product_name", new AttributeValue { S = record.ProductName } },
                { "seller_id", new AttributeValue { S = record.SellerId } },
                { "supplier_id", new AttributeValue() { S = supplierId} },
                { "marketplace", new AttributeValue { S = record.Marketplace } },
                { "store_id", new AttributeValue { N = record.StoreId.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { "sku_count", new AttributeValue { N = record.SkuCount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { "created_at", new AttributeValue { S = createdAtUtc } }
            };

            await _repository.PutItemAsync(item);

            _logger.LogInformation("Product-seller META created - ProductId: {ProductId}, SellerId: {SellerId}",
                productId, sellerId);

            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product-seller META - ProductId: {ProductId}, SellerId: {SellerId}",
                productId, sellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os produtos vinculados a um vendedor em um marketplace específico
    /// </summary>
    public async Task<List<ProductSellerDomain>> GetProductsBySellerAsync(string sellerId)
    {
        _logger.LogInformation("Getting products for seller - SellerId: {SellerId}", sellerId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Seller#shopee#{sellerId}" } },
                { ":sk_prefix", new AttributeValue { S = "Product#" } }
            };

            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_prefix)",
                expressionAttributeValues: expressionAttributeValues
            );

            if (items == null || items.Count == 0)
            {
                _logger.LogDebug("No products found for seller - SellerId: {SellerId}", sellerId);
                return new List<ProductSellerDomain>();
            }

            var products = items.Select(ProductSellerMapper.ToDomain).ToList();

            _logger.LogInformation("Found {Count} products for seller - SellerId: {SellerId}",
                products.Count, sellerId);

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um registro específico de Product-Seller
    /// </summary>
    public async Task<ProductSellerDomain?> GetProductSellerAsync(string sellerId, string marketplace, string productId)
    {
        _logger.LogInformation("Getting product-seller record - SellerId: {SellerId}, ProductId: {ProductId}",
            sellerId, productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Seller#{marketplace}#{sellerId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            var item = await _repository.GetItemAsync(key);
            return item != null ? ProductSellerMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product-seller record - SellerId: {SellerId}, ProductId: {ProductId}",
                sellerId, productId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza a contagem de SKUs de um produto-vendedor
    /// </summary>
    public async Task<ProductSellerDomain?> UpdateSkuCountAsync(string sellerId, string marketplace, string productId, int skuCount)
    {
        _logger.LogInformation("Updating SKU count - SellerId: {SellerId}, ProductId: {ProductId}, Count: {Count}",
            sellerId, productId, skuCount);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Seller#{marketplace}#{sellerId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            var updateExpression = "SET sku_count = :count, updated_at = :updated_at";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":count", new AttributeValue { N = skuCount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { ":updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await _repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            var item = await _repository.GetItemAsync(key);
            return item != null ? ProductSellerMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SKU count - SellerId: {SellerId}, ProductId: {ProductId}",
                sellerId, productId);
            throw;
        }
    }

    /// <summary>
    /// Remove um produto da lista de produtos vinculados a um vendedor
    /// </summary>
    public async Task<bool> RemoveProductSellerAsync(string sellerId, string marketplace, string productId)
    {
        _logger.LogInformation("Removing product from seller - SellerId: {SellerId}, ProductId: {ProductId}",
            sellerId, productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Seller#{marketplace}#{sellerId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            await _repository.DeleteItemAsync(key);

            _logger.LogInformation("Product removed from seller - SellerId: {SellerId}, ProductId: {ProductId}",
                sellerId, productId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product from seller - SellerId: {SellerId}, ProductId: {ProductId}",
                sellerId, productId);
            throw;
        }
    }

    public async Task UpdateMarketplaceItemIdAsync(ProductSellerDomain productSeller)
    {
        _logger.LogInformation("Updating marketplace item ID - SellerId: {SellerId}, ProductId: {ProductId}, MarketplaceItemId: {MarketplaceItemId}",
            productSeller.SellerId, productSeller.ProductId, productSeller.MarketplaceItemId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = productSeller.Pk } },
                { "SK", new AttributeValue { S = productSeller.Sk } }
            };

            var updateExpression = "SET marketplace_item_id = :marketplace_item_id, updated_at = :updated_at";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":marketplace_item_id", new AttributeValue { N = productSeller.MarketplaceItemId.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { ":updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await _repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            _logger.LogInformation("Marketplace item ID updated - SellerId: {SellerId}, ProductId: {ProductId}, MarketplaceItemId: {MarketplaceItemId}",
                productSeller.SellerId, productSeller.ProductId, productSeller.MarketplaceItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating marketplace item ID - SellerId: {SellerId}, ProductId: {ProductId}",
                productSeller.SellerId, productSeller.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza o marketplace_model_id de um SKU de vendedor
    /// </summary>
    public async Task UpdateMarketplaceModelIdAsync(
        string productId,
        string sku,
        string sellerId,
        string marketplace,
        string modelId,
        string itemId)
    {
        _logger.LogInformation("Updating marketplace model ID - ProductId: {ProductId}, SKU: {Sku}, ModelId: {ModelId}",
            productId, sku, modelId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Seller#{marketplace}#{sellerId}" } }
            };

            var updateExpression = "SET marketplace_model_id = :model_id, marketplace_item_id = :item_id, updated_at = :updated_at";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":model_id", new AttributeValue { S = modelId } },
                { ":item_id", new AttributeValue { S = itemId } },
                { ":updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await _repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            _logger.LogInformation("Marketplace model ID updated - ProductId: {ProductId}, SKU: {Sku}, ModelId: {ModelId}",
                productId, sku, modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating marketplace model ID - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            throw;
        }
    }
}
