using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para gerenciar relações entre Produtos, SKUs e Vendedores (Marketplace)
/// Permite vincular vendedores com produtos e gerenciar preços
/// </summary>
public class ProductSkuSellerRepository(DynamoDbRepository repository, ILogger<ProductSkuSellerRepository> logger)
{
    /// <summary>
    /// Vincula um vendedor a um produto com preço
    /// Cria um registro para cada SKU do produto
    /// </summary>
    public async Task<List<ProductSkuSellerDomain>> LinkSellerToProductAsync(
        string productId,
        string sellerId,
        string marketplace,
        long storeId,
        decimal price,
        List<SkuDomain> skus)
    {
        logger.LogInformation("Linking seller to product - ProductId: {ProductId}, SellerId: {SellerId}, Marketplace: {Marketplace}",
            productId, sellerId, marketplace);

        var createdRecords = new List<ProductSkuSellerDomain>();

        try
        {
            // Criar um registro para cada SKU
            foreach (var sku in skus)
            {
                var record = await CreateProductSkuSellerAsync(productId, sku.Sku, sellerId, marketplace, storeId,"", "", price);
                createdRecords.Add(record);

                logger.LogDebug("Created product-sku-seller link - ProductId: {ProductId}, SKU: {Sku}, SellerId: {SellerId}",
                    productId, sku, sellerId);
            }

            logger.LogInformation("Successfully linked {Count} SKUs to seller - ProductId: {ProductId}, SellerId: {SellerId}",
                createdRecords.Count, productId, sellerId);

            return createdRecords;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error linking seller to product - ProductId: {ProductId}, SellerId: {SellerId}",
                productId, sellerId);
            throw;
        }
    }

    /// <summary>
    /// Cria um registro individual de Product-SKU-Seller
    /// </summary>
    private async Task<ProductSkuSellerDomain> CreateProductSkuSellerAsync(
        string productId,
        string sku,
        string sellerId,
        string marketplace,
        long storeId,
        string marketplaceProductId,
        string marketplaceModelId,
        decimal price)
    {
        var createdAtUtc = DateTime.UtcNow.ToString("O");

        var record = new ProductSkuSellerDomain
        {
            Pk = $"Product#{productId}",
            Sk = $"Sku#{sku}#Seller#{marketplace}#{sellerId}",
            EntityType = "product_sku_seller",
            ProductId = productId,
            Sku = sku,
            SellerId = sellerId,
            Marketplace = marketplace,
            StoreId = storeId,
            MarketplaceProductId = marketplaceProductId,
            MarketplaceModelId = marketplaceModelId,
            Price = price,
            Quantity = 0, // Será atualizado automaticamente via sistema
            CreatedAt = DateTime.UtcNow
        };

        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = record.Pk } },
            { "SK", new AttributeValue { S = record.Sk } },
            { "entity_type", new AttributeValue { S = record.EntityType } },
            { "product_id", new AttributeValue { S = record.ProductId } },
            { "sku", new AttributeValue { S = record.Sku } },
            { "seller_id", new AttributeValue { S = record.SellerId } },
            { "marketplace", new AttributeValue { S = record.Marketplace } },
            { "store_id", new AttributeValue { N = record.StoreId.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
            { "marketplace_product_id", new AttributeValue { S = record.MarketplaceProductId } },
            { "marketplace_model_id", new AttributeValue { S = record.MarketplaceModelId } },
            { "price", new AttributeValue { N = record.Price.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
            { "quantity", new AttributeValue { N = record.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
            { "created_at", new AttributeValue { S = createdAtUtc } }
        };

        await repository.PutItemAsync(item);

        return record;
    }

    /// <summary>
    /// Obtém todos os SKUs de um produto vinculados a um vendedor específico
    /// </summary>
    public async Task<List<ProductSkuSellerDomain>> GetSkusBySellerAsync(string productId, string sellerId, string marketplace = "shopee")
    {
        logger.LogInformation("Getting SKUs for seller - ProductId: {ProductId}, SellerId: {SellerId}, Marketplace: {Marketplace}",
            productId, sellerId, marketplace);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Product#{productId}" } },
                { ":sk_prefix", new AttributeValue { S = $"Sku#" } },
                { ":seller", new AttributeValue { S = sellerId } }
            };

            var items = await repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_prefix)",
                expressionAttributeValues: expressionAttributeValues,
                filterExpression: "seller_id = :seller"
            );

            if (items == null || items.Count == 0)
            {
                logger.LogDebug("No SKUs found for seller - ProductId: {ProductId}, SellerId: {SellerId}",
                    productId, sellerId);
                return new List<ProductSkuSellerDomain>();
            }

            var skus = items.Select(ProductSkuSellerMapper.ToDomain).ToList();
            return skus;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting SKUs for seller - ProductId: {ProductId}, SellerId: {SellerId}",
                productId, sellerId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza o preço de um SKU
    /// </summary>
    public async Task<ProductSkuSellerDomain?> UpdatePriceAsync(
        string productId,
        string sku,
        string sellerId,
        string marketplace,
        decimal price)
    {
        logger.LogInformation("Updating seller price - ProductId: {ProductId}, SKU: {Sku}, Price: {Price}",
            productId, sku, price);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Seller#{marketplace}#{sellerId}" } }
            };

            var updateExpression = "SET price = :price, updated_at = :updated_at";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":price", new AttributeValue { N = price.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { ":updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            var item = await repository.GetItemAsync(key);
            return item != null ? ProductSkuSellerMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating price - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            throw;
        }
    }

    /// <summary>
    /// Atualiza a quantidade de um SKU (via sistema automaticamente)
    /// </summary>
    public async Task<ProductSkuSellerDomain?> UpdateQuantityAsync(
        string productId,
        string sku,
        string sellerId,
        string marketplace,
        long quantity)
    {
        logger.LogInformation("Updating seller quantity - ProductId: {ProductId}, SKU: {Sku}, Quantity: {Quantity}",
            productId, sku, quantity);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Seller#{marketplace}#{sellerId}" } }
            };

            var updateExpression = "SET quantity = :qty, updated_at = :updated_at";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":qty", new AttributeValue { N = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { ":updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            var item = await repository.GetItemAsync(key);
            return item != null ? ProductSkuSellerMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating quantity - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            throw;
        }
    }

    /// <summary>
    /// Remove um vendedor de um SKU
    /// </summary>
    public async Task<bool> RemoveSellerFromSkuAsync(string productId, string sku, string sellerId, string marketplace)
    {
        logger.LogInformation("Removing seller from SKU - ProductId: {ProductId}, SKU: {Sku}, SellerId: {SellerId}",
            productId, sku, sellerId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Seller#{marketplace}#{sellerId}" } }
            };

            await repository.DeleteItemAsync(key);

            logger.LogInformation("Seller removed from SKU - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing seller from SKU - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            throw;
        }
    }

    /// <summary>
    /// Obtém um registro específico de Product-SKU-Seller
    /// </summary>
    public async Task<ProductSkuSellerDomain?> GetProductSkuSellerAsync(
        string productId, string sku, string sellerId, string marketplace)
    {
        logger.LogInformation("Getting product-sku-seller record - ProductId: {ProductId}, SKU: {Sku}, SellerId: {SellerId}",
            productId, sku, sellerId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Seller#{marketplace}#{sellerId}" } }
            };

            var item = await repository.GetItemAsync(key);
            return item != null ? ProductSkuSellerMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting product-sku-seller record - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            throw;
        }
    }
}

