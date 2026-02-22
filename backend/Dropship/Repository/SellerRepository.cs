using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com Seller no DynamoDB
/// </summary>
public class SellerRepository
{
    private readonly IDynamoDBContext _context;
    private readonly DynamoDbRepository _dynamoDbRepository;
    private readonly ILogger<SellerRepository> _logger;
    private readonly IMemoryCache _memoryCache;
    
    // Tempo de expiração do cache em minutos
    private const int CacheExpirationMinutes = 5;

    public SellerRepository(
        IDynamoDBContext context, 
        DynamoDbRepository dynamoDbRepository, 
        ILogger<SellerRepository> logger,
        IMemoryCache memoryCache)
    {
        _context = context;
        _dynamoDbRepository = dynamoDbRepository;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<bool> VerifyIfShopExistsAsync(long shopId)
    {
        var shop = await GetSellerByShopIdAsync(shopId);
        
        return shop != null;
    }

    /// <summary>
    /// Obtém um vendedor pelo ID
    /// </summary>
    public async Task<SellerDomain?> GetSellerByIdAsync(string sellerId)
    {
        _logger.LogInformation("Getting seller by ID - SellerId: {SellerId}", sellerId);

        try
        {
            var pk = $"Seller#{sellerId}";
            var seller = await _context.LoadAsync<SellerDomain>(pk, "META");

            if (seller != null)
            {
                _logger.LogInformation("Seller found - SellerId: {SellerId}", sellerId);
            }
            else
            {
                _logger.LogWarning("Seller not found - SellerId: {SellerId}", sellerId);
            }

            return seller;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um vendedor pelo Shop ID
    /// Usa cache em memória com expiração de 5 minutos para otimizar performance
    /// Se não estiver em cache, busca no DynamoDB (GSI_SHOPID_LOOKUP)
    /// </summary>
    public async Task<SellerDomain?> GetSellerByShopIdAsync(long shopId)
    {
        _logger.LogInformation("Getting seller by shop ID - ShopId: {ShopId}", shopId);

        try
        {
            // Chave do cache: "Seller_ShopId_{shopId}"
            var cacheKey = $"Seller_ShopId_{shopId}";

            // Verificar se está em cache
            if (_memoryCache.TryGetValue(cacheKey, out SellerDomain? cachedSeller))
            {
                _logger.LogInformation("Seller found in cache - ShopId: {ShopId}, SellerId: {SellerId}", 
                    shopId, cachedSeller?.SellerId);
                return cachedSeller;
            }

            _logger.LogInformation("Seller not in cache, querying DynamoDB - ShopId: {ShopId}", shopId);

            // Query usando DynamoDbRepository
            var items = await _dynamoDbRepository.QueryTableAsync(
                keyConditionExpression: "shop_id = :shopid AND begins_with(PK, :pk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":shopid", new AttributeValue { N = shopId.ToString() } },
                    { ":pk", new AttributeValue() { S = "Seller#" } }
                },
                indexName: "GSI_SHOPID_LOOKUP"
            );

            if (items.Count == 0)
            {
                _logger.LogWarning("Seller not found by shop ID - ShopId: {ShopId}", shopId);
                return null;
            }

            // Mapear o resultado
            var item = items[0];
            var seller = MapDynamoDbItemToSeller(item);

            // Armazenar em cache com expiração de 5 minutos
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes));
            
            _memoryCache.Set(cacheKey, seller, cacheOptions);

            _logger.LogInformation("Seller found and cached - ShopId: {ShopId}, SellerId: {SellerId}, CacheDuration: {Minutes}min", 
                shopId, seller.SellerId, CacheExpirationMinutes);
            
            return seller;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seller by shop ID - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Mapeia um item do DynamoDB para SellerDomain
    /// Mapeamento manual sem usar annotations
    /// </summary>
    private static SellerDomain MapDynamoDbItemToSeller(Dictionary<string, AttributeValue> item)
    {
        return new SellerDomain
        {
            // Chaves
            PK = item.ContainsKey("PK") ? item["PK"].S : string.Empty,
            SK = item.ContainsKey("SK") ? item["SK"].S : string.Empty,

            // Identificadores
            SellerId = item.ContainsKey("sellerId") ? item["sellerId"].S : 
                      (item.ContainsKey("seller_id") ? item["seller_id"].S : string.Empty),
            
            // Informações Básicas
            SellerName = item.ContainsKey("sellerName") ? item["sellerName"].S : 
                        (item.ContainsKey("seller_name") ? item["seller_name"].S : string.Empty),
            
            // Shop Info
            ShopId = item.ContainsKey("shop_id") && item["shop_id"].N != null 
                ? long.Parse(item["shop_id"].N) 
                : 0,
            
            // Marketplace
            Marketplace = item.ContainsKey("marketplace") ? item["marketplace"].S : "shopee",
            
            // Entity Type
            EntityType = item.ContainsKey("entityType") ? item["entityType"].S : 
                        (item.ContainsKey("entity_type") ? item["entity_type"].S : "seller"),
            
            // Metadata
            CreatedAt = item.ContainsKey("createdAt") && item["createdAt"].N != null 
                ? long.Parse(item["createdAt"].N) 
                : (item.ContainsKey("created_at") && item["created_at"].N != null 
                    ? long.Parse(item["created_at"].N) 
                    : DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            
            UpdatedAt = item.ContainsKey("updatedAt") && item["updatedAt"].N != null 
                ? long.Parse(item["updatedAt"].N) 
                : (item.ContainsKey("updated_at") && item["updated_at"].N != null 
                    ? long.Parse(item["updated_at"].N) 
                    : DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };
    }

    /// <summary>
    /// Invalida (remove) o cache para um seller específico
    /// Deve ser chamado quando um seller é criado, atualizado ou deletado
    /// </summary>
    private void InvalidateSellerCache(SellerDomain seller)
    {
        var cacheKey = $"Seller_ShopId_{seller.ShopId}";
        _memoryCache.Remove(cacheKey);
        _logger.LogInformation("Seller cache invalidated - ShopId: {ShopId}, SellerId: {SellerId}", 
            seller.ShopId, seller.SellerId);
    }

    /// <summary>
    /// Cria um novo vendedor
    /// </summary>
    public async Task<SellerDomain> CreateSellerAsync(SellerDomain seller)
    {
        _logger.LogInformation("Creating seller - SellerId: {SellerId}, ShopId: {ShopId}", 
            seller.SellerId, seller.ShopId);

        try
        {
            // Validação básica
            if (string.IsNullOrWhiteSpace(seller.SellerId))
                throw new ArgumentException("SellerId is required");

            if (string.IsNullOrWhiteSpace(seller.SellerName))
                throw new ArgumentException("SellerName is required");

            if (seller.ShopId <= 0)
                throw new ArgumentException("ShopId must be greater than 0");

            // Definir valores padrão
            seller.PK = $"Seller#{seller.SellerId}";
            seller.SK = "META";
            seller.EntityType = "seller";
            seller.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            seller.UpdatedAt = seller.CreatedAt;

            await _context.SaveAsync(seller);

            // Invalidar cache após criar
            InvalidateSellerCache(seller);

            _logger.LogInformation("Seller created successfully - SellerId: {SellerId}", seller.SellerId);
            return seller;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating seller - SellerId: {SellerId}", seller.SellerId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza um vendedor existente
    /// </summary>
    public async Task<SellerDomain> UpdateSellerAsync(SellerDomain seller)
    {
        _logger.LogInformation("Updating seller - SellerId: {SellerId}", seller.SellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(seller.SellerId))
                throw new ArgumentException("SellerId is required");

            // Verificar se o vendedor existe
            var existingSeller = await GetSellerByIdAsync(seller.SellerId);
            if (existingSeller == null)
                throw new InvalidOperationException($"Seller with ID {seller.SellerId} not found");

            // Manter PK, SK, EntityType e CreatedAt inalterados
            seller.PK = existingSeller.PK;
            seller.SK = existingSeller.SK;
            seller.EntityType = existingSeller.EntityType;
            seller.CreatedAt = existingSeller.CreatedAt;
            seller.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await _context.SaveAsync(seller);

            // Invalidar cache após atualizar
            InvalidateSellerCache(seller);

            _logger.LogInformation("Seller updated successfully - SellerId: {SellerId}", seller.SellerId);
            return seller;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating seller - SellerId: {SellerId}", seller.SellerId);
            throw;
        }
    }

    /// <summary>
    /// Verifica se um vendedor existe pelo ID
    /// </summary>
    public async Task<bool> SellerExistsAsync(string sellerId)
    {
        _logger.LogInformation("Checking if seller exists - SellerId: {SellerId}", sellerId);

        try
        {
            var seller = await GetSellerByIdAsync(sellerId);
            return seller != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking seller existence - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Verifica se um vendedor existe pelo Shop ID
    /// </summary>
    public async Task<bool> SellerExistsByShopIdAsync(long shopId)
    {
        _logger.LogInformation("Checking if seller exists by shop ID - ShopId: {ShopId}", shopId);

        try
        {
            var seller = await GetSellerByShopIdAsync(shopId);
            return seller != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking seller existence by shop ID - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Remove um vendedor
    /// </summary>
    public async Task DeleteSellerAsync(string sellerId)
    {
        _logger.LogInformation("Deleting seller - SellerId: {SellerId}", sellerId);

        try
        {
            // Verificar se o vendedor existe
            var seller = await GetSellerByIdAsync(sellerId);
            if (seller == null)
                throw new InvalidOperationException($"Seller with ID {sellerId} not found");

            await _context.DeleteAsync(seller);

            // Invalidar cache após deletar
            InvalidateSellerCache(seller);

            _logger.LogInformation("Seller deleted successfully - SellerId: {SellerId}", sellerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }
}
