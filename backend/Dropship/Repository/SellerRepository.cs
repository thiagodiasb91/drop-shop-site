using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com Seller no DynamoDB
/// Usa DynamoDbRepository diretamente e SellerMapper para conversão
/// </summary>
public class SellerRepository(
    DynamoDbRepository dynamoDbRepository,
    ILogger<SellerRepository> logger,
    IMemoryCache memoryCache)
{
    private const int CacheExpirationMinutes = 5;

    public async Task<bool> VerifyIfShopExistsAsync(long shopId)
        => await GetSellerByShopIdAsync(shopId) != null;

    /// <summary>
    /// Obtém um vendedor pelo SellerId
    /// PK = Seller#{sellerId} | SK = META
    /// </summary>
    public async Task<SellerDomain?> GetSellerByIdAsync(string sellerId)
    {
        logger.LogInformation("Getting seller by ID - SellerId: {SellerId}", sellerId);

        try
        {
            var items = await dynamoDbRepository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND SK = :sk",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Seller#{sellerId}" } },
                    { ":sk", new AttributeValue { S = "META" } }
                }
            );

            if (items == null || items.Count == 0)
            {
                logger.LogWarning("Seller not found - SellerId: {SellerId}", sellerId);
                return null;
            }

            return SellerMapper.ToDomain(items.First());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um vendedor pelo ShopId
    /// Usa MemoryCache com expiração de 5 minutos
    /// </summary>
    public async Task<SellerDomain?> GetSellerByShopIdAsync(long shopId)
    {
        logger.LogInformation("Getting seller by ShopId - ShopId: {ShopId}", shopId);

        try
        {
            var cacheKey = $"Seller_ShopId_{shopId}";

            if (memoryCache.TryGetValue(cacheKey, out SellerDomain? cached))
            {
                logger.LogInformation("Seller found in cache - ShopId: {ShopId}", shopId);
                return cached;
            }

            var items = await dynamoDbRepository.QueryTableAsync(
                keyConditionExpression: "shop_id = :shopid AND begins_with(PK, :pk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":shopid", new AttributeValue { N = shopId.ToString() } },
                    { ":pk",     new AttributeValue { S = "Seller#" } }
                },
                indexName: "GSI_SHOPID_LOOKUP"
            );

            if (items == null || items.Count == 0)
            {
                logger.LogWarning("Seller not found by ShopId - ShopId: {ShopId}", shopId);
                return null;
            }

            var seller =SellerMapper.ToDomain(items.First());

            memoryCache.Set(cacheKey, seller,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes)));

            logger.LogInformation("Seller cached - ShopId: {ShopId}, SellerId: {SellerId}", shopId, seller.SellerId);
            return seller;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting seller by ShopId - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Cria um novo vendedor
    /// </summary>
    public async Task<SellerDomain> CreateSellerAsync(SellerDomain seller)
    {
        logger.LogInformation("Creating seller - SellerId: {SellerId}", seller.SellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(seller.SellerId)) throw new ArgumentException("SellerId is required");
            if (string.IsNullOrWhiteSpace(seller.SellerName)) throw new ArgumentException("SellerName is required");
            if (seller.ShopId <= 0) throw new ArgumentException("ShopId must be greater than 0");

            seller.PK        = $"Seller#{seller.SellerId}";
            seller.SK        = "META";
            seller.EntityType = "seller";
            seller.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            seller.UpdatedAt = seller.CreatedAt;

            await dynamoDbRepository.PutItemAsync(seller.ToDynamoDb());

            InvalidateCache(seller);
            logger.LogInformation("Seller created - SellerId: {SellerId}", seller.SellerId);
            return seller;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating seller - SellerId: {SellerId}", seller.SellerId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza um vendedor existente
    /// </summary>
    public async Task<SellerDomain> UpdateSellerAsync(SellerDomain seller)
    {
        logger.LogInformation("Updating seller - SellerId: {SellerId}", seller.SellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(seller.SellerId)) throw new ArgumentException("SellerId is required");

            var existing = await GetSellerByIdAsync(seller.SellerId)
                ?? throw new InvalidOperationException($"Seller {seller.SellerId} not found");

            seller.PK        = existing.PK;
            seller.SK        = existing.SK;
            seller.EntityType = existing.EntityType;
            seller.CreatedAt = existing.CreatedAt;
            seller.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await dynamoDbRepository.PutItemAsync(seller.ToDynamoDb());

            InvalidateCache(seller);
            logger.LogInformation("Seller updated - SellerId: {SellerId}", seller.SellerId);
            return seller;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating seller - SellerId: {SellerId}", seller.SellerId);
            throw;
        }
    }

    /// <summary>
    /// Remove um vendedor
    /// </summary>
    public async Task DeleteSellerAsync(string sellerId)
    {
        logger.LogInformation("Deleting seller - SellerId: {SellerId}", sellerId);

        try
        {
            var seller = await GetSellerByIdAsync(sellerId)
                ?? throw new InvalidOperationException($"Seller {sellerId} not found");

            await dynamoDbRepository.DeleteItemAsync(new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = seller.PK } },
                { "SK", new AttributeValue { S = seller.SK } }
            });

            InvalidateCache(seller);
            logger.LogInformation("Seller deleted - SellerId: {SellerId}", sellerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    public async Task<bool> SellerExistsAsync(string sellerId)
        => await GetSellerByIdAsync(sellerId) != null;

    public async Task<bool> SellerExistsByShopIdAsync(long shopId)
        => await GetSellerByShopIdAsync(shopId) != null;

    // ── private ─────────────────────────────────────────────────────────────────
    private void InvalidateCache(SellerDomain seller)
    {
        memoryCache.Remove($"Seller_ShopId_{seller.ShopId}");
        logger.LogInformation("Seller cache invalidated - ShopId: {ShopId}", seller.ShopId);
    }
}
