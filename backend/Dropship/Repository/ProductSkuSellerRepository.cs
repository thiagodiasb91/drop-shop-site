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
    /// Vincula um vendedor a um produto
    /// Cria um registro para cada domain ProductSkuSeller fornecido
    /// </summary>
    public async Task<List<ProductSkuSellerDomain>> LinkSellerToProductAsync(
        List<ProductSkuSellerDomain> domains)
    {
        if (domains == null || domains.Count == 0)
        {
            logger.LogWarning("No domains provided for linking");
            return new List<ProductSkuSellerDomain>();
        }

        var firstDomain = domains.First();
        logger.LogInformation("Linking seller to product - ProductId: {ProductId}, SellerId: {SellerId}, Count: {Count}",
            firstDomain.ProductId, firstDomain.SellerId, domains.Count);

        var createdRecords = new List<ProductSkuSellerDomain>();

        try
        {
            foreach (var domain in domains)
            {
                var record = await CreateProductSkuSellerAsync(domain);
                createdRecords.Add(record);

                logger.LogDebug("Created product-sku-seller link - ProductId: {ProductId}, SKU: {Sku}, SellerId: {SellerId}",
                    domain.ProductId, domain.Sku, domain.SellerId);
            }

            logger.LogInformation("Successfully linked {Count} SKUs to seller - ProductId: {ProductId}, SellerId: {SellerId}",
                createdRecords.Count, firstDomain.ProductId, firstDomain.SellerId);

            return createdRecords;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error linking seller to product - Count: {Count}", domains.Count);
            throw;
        }
    }

    /// <summary>
    /// Cria um registro de Product-SKU-Seller no banco
    /// Recebe o domain já construído
    /// </summary>
    private async Task<ProductSkuSellerDomain> CreateProductSkuSellerAsync(ProductSkuSellerDomain domain)
    {
        var createdAtUtc = DateTime.UtcNow.ToString("O");

        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.Pk } },
            { "SK", new AttributeValue { S = domain.Sk } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "product_id", new AttributeValue { S = domain.ProductId } },
            { "sku", new AttributeValue { S = domain.Sku } },
            { "supplier_id", new AttributeValue() { S = domain.SupplierId} },
            { "seller_id", new AttributeValue { S = domain.SellerId } },
            { "marketplace", new AttributeValue { S = domain.Marketplace } },
            { "store_id", new AttributeValue { N = domain.StoreId.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
            { "marketplace_product_id", new AttributeValue { S = domain.MarketplaceProductId } },
            { "marketplace_model_id", new AttributeValue { S = domain.MarketplaceModelId } },
            { "price", new AttributeValue { N = domain.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) } },
            { "quantity", new AttributeValue { N = domain.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
            { "created_at", new AttributeValue { S = createdAtUtc } }
        };

        await repository.PutItemAsync(item);

        return domain;
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

