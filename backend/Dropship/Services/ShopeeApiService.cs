using System.Text;
using System.Text.Json;
using Dropship.Responses;
using Dropship.Services.Helpers;

namespace Dropship.Services;

/// <summary>
/// Serviço para autenticação e conexão com API da Shopee
/// Implementa fluxo de OAuth2 com refresh token e cache de tokens
/// </summary>
public class ShopeeApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopeeApiService> _logger;
    private readonly ICacheService _cacheService;
    private readonly string _partnerId;
    private readonly string _partnerKey;

    // Configurações da Shopee - Sandbox Environment
    private const string SandboxApiHost = "https://openplatform.sandbox.test-stable.shopee.sg";
    
    private const string DefaultApiHost = SandboxApiHost;

    public ShopeeApiService(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger<ShopeeApiService> logger)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _logger = logger;

        // Obter credenciais de variáveis de ambiente (com fallback para valores de sandbox)
        _partnerId = Environment.GetEnvironmentVariable("SHOPEE_PARTNER_ID") ?? "1203628";
        _partnerKey = Environment.GetEnvironmentVariable("SHOPEE_PARTNER_KEY") ?? "shpk4871546d53586b746b4c57614a4b5a577a4476726a4e6747765749665468";
    }

    /// <summary>
    /// Gera URL de autenticação para o fluxo OAuth2 com Shopee
    /// Baseado no código Python fornecido
    /// Utiliza o endpoint /api/v2/shop/auth_partner (Legacy)
    /// 
    /// Formato base string: {partner_id}{path}{timestamp}
    /// Exemplo: 1203628/api/v2/shop/auth_partner1706901234
    /// </summary>
    /// <param name="email">Email do seller para autorização (será passado como parâmetro na redirect URL)</param>
    /// <param name="requestUri">URI base da aplicação para construir a URL de callback</param>
    /// <returns>URL de autorização com assinatura HMAC SHA256</returns>
    public string GetAuthUrl(string email, string requestUri)
    {
        _logger.LogInformation("Generating auth URL - PartnerId: {PartnerId}, Email: {Email}", _partnerId, email);

        try
        {
            if (string.IsNullOrWhiteSpace(_partnerId))
            {
                _logger.LogError("Partner ID is empty or null");
                throw new InvalidOperationException("Partner ID is required");
            }
            
            if (string.IsNullOrWhiteSpace(_partnerKey))
            {
                _logger.LogError("Partner Key is empty or null");
                throw new InvalidOperationException("Partner Key is required");
            }

            var path = "/api/v2/shop/auth_partner";
            
            var timest = ShopeeApiHelper.GetCurrentTimestamp();
            
            // Construir redirect URI dinâmica com o email
            var redirectUrl = $"{requestUri}/sellers/{Uri.EscapeDataString(email)}/store/code";
            
            _logger.LogDebug("HMAC Input - PartnerId: {PartnerId}, Path: {Path}, Timestamp: {Timestamp}", _partnerId, path, timest);
            
            // Gerar assinatura usando helper estático
            var sign = ShopeeApiHelper.GenerateSign(_partnerId, _partnerKey, path, timest);
                
            _logger.LogDebug("HMAC Sign generated: {Sign}", sign);
                
            var sandboxHost = "https://openplatform.sandbox.test-stable.shopee.sg";
            var url = $"{sandboxHost}{path}?partner_id={_partnerId}&redirect={Uri.EscapeDataString(redirectUrl)}&timestamp={timest}&sign={sign}";
                
            _logger.LogInformation("Auth URL generated successfully - Host: {Host}, PartnerId: {PartnerId}, Timestamp: {Timestamp}, Sign: {Sign}", sandboxHost, _partnerId, timest, sign);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating auth URL - Email: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Obtém token em nível de loja (shop-level) usando o código de autorização
    /// Usa o endpoint de API openplatform.sandbox.test-stable.shopee.sg
    /// </summary>
    private async Task<(string AccessToken, string RefreshToken, long ExpiresIn)> GetTokenShopLevelAsync(string code, long shopId)
    {
        _logger.LogInformation("Getting shop-level token - ShopId: {ShopId}", shopId);

        try
        {
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/auth/token/get";
            var sign = ShopeeApiHelper.GenerateSign(_partnerId, _partnerKey, path, timestamp);

            // Usar API host para chamadas de token, não account host
            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}";

            var body = new
            {
                code,
                shop_id = shopId,
                partner_id = _partnerId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Shopee API error - ShopId: {ShopId}, StatusCode: {StatusCode}, Error: {Error}",
                    shopId, response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to get token: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            // Verificar se há erro na resposta
            if (responseJson.RootElement.TryGetProperty("error", out var errorElement) && !string.IsNullOrEmpty(errorElement.GetString()))
            {
                var errorMessage = responseJson.RootElement.TryGetProperty("message", out var msgElement)
                    ? msgElement.GetString()
                    : "Unknown error";

                _logger.LogWarning("Shopee error response - ShopId: {ShopId}, Error: {Error}, Message: {Message}",
                    shopId, errorElement.GetString(), errorMessage);
                throw new InvalidOperationException($"Error fetching token: {errorMessage}");
            }

            // Extrair tokens usando helper
            var accessToken = ShopeeApiHelper.GetJsonProperty(responseJson, "access_token");
            var refreshToken = ShopeeApiHelper.GetJsonProperty(responseJson, "refresh_token");
            var expiresIn = ShopeeApiHelper.ParseExpiresIn(responseJson);

            _logger.LogInformation("Shop-level token obtained successfully - ShopId: {ShopId}, ExpiresIn: {ExpiresIn}",
                shopId, expiresIn);

            return (accessToken, refreshToken, expiresIn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shop-level token - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um token de acesso válido, usando cache quando possível
    /// Implementação baseada no padrão Python:
    /// - Retorna token em cache se ainda válido
    /// - Tenta fazer refresh se expirado
    /// - Faz nova troca de token completa se refresh falhar
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="code">Authorization code (obrigatório apenas se não houver token em cache)</param>
    /// <returns>Token de acesso válido</returns>
    public async Task<string> GetCachedAccessTokenAsync(long shopId, string? code = null)
    {
        _logger.LogInformation("GetCachedAccessToken - ShopId: {ShopId}", shopId);

        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var accessTokenKey = $"{shopId}_access_token";
            var refreshTokenKey = $"{shopId}_refresh_token";
            var expiresAtKey = $"{shopId}_access_token_expires_at";

            if (!string.IsNullOrEmpty(code))
            {
                _logger.LogInformation("Obtaining new access token via full exchange - ShopId: {ShopId}", shopId);
                var (newToken, newRefreshToken, expiresIn) = await GetTokenShopLevelAsync(code, shopId);

                if (string.IsNullOrEmpty(newToken))
                {
                    _logger.LogError("Failed to obtain new access token - ShopId: {ShopId}", shopId);
                    throw new InvalidOperationException("Failed to obtain access token");
                }

                var newExpiresAt = now + expiresIn;

                _logger.LogDebug("Caching new tokens - ShopId: {ShopId}, ExpiresIn: {ExpiresIn}s", shopId, expiresIn);
                await _cacheService.SaveManyAsync(
                    (accessTokenKey, newToken),
                    (refreshTokenKey, newRefreshToken),
                    (expiresAtKey, newExpiresAt.ToString())
                );

                _logger.LogInformation("New access token cached successfully - ShopId: {ShopId}", shopId);
                return newToken;
            }

            // Buscar tokens em cache
            _logger.LogDebug("Fetching tokens from cache - ShopId: {ShopId}", shopId);
            var cached = await _cacheService.GetManyAsync(accessTokenKey, refreshTokenKey, expiresAtKey);

            var accessToken = cached.ContainsKey(accessTokenKey) ? cached[accessTokenKey] : null;
            var refreshToken = cached.ContainsKey(refreshTokenKey) ? cached[refreshTokenKey] : null;
            var expiresAtStr = cached.ContainsKey(expiresAtKey) ? cached[expiresAtKey] : null;

            // Parsear expires_at
            long expiresAt = 0;
            if (!string.IsNullOrEmpty(expiresAtStr) && long.TryParse(expiresAtStr, out var parsedExpires))
            {
                expiresAt = parsedExpires;
            }

            if (!string.IsNullOrEmpty(accessToken) && now < expiresAt)
            {
                _logger.LogInformation("Using cached access token - ShopId: {ShopId}, ExpiresIn: {ExpiresIn}s", shopId, expiresAt - now);
                return accessToken;
            }

            _logger.LogInformation("Cached token expired or not found - ShopId: {ShopId}", shopId);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogInformation("Attempting to refresh access token - ShopId: {ShopId}", shopId);
                try
                {
                    var newAccessToken = await RefreshAccessTokenAsync(shopId, refreshToken);
                    _logger.LogInformation("Access token refreshed successfully - ShopId: {ShopId}", shopId);
                    return newAccessToken;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Refresh token failed - ShopId: {ShopId}, will perform full token exchange", shopId);
                }
            }
            
            _logger.LogInformation("Performing full token exchange due to missing/invalid refresh token - ShopId: {ShopId}", shopId);
            throw new InvalidOperationException("Unable to obtain valid access token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached access token - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Faz refresh do access token usando o refresh token
    /// Baseado na implementação Python com melhorias
    /// Se o refresh falhar, lança Exception para que o caller possa fazer full token exchange
    /// Nota: Este é um endpoint shop-level que requer access_token na assinatura
    /// </summary>
    private async Task<string> RefreshAccessTokenAsync(long shopId, string refreshToken)
    {
        _logger.LogInformation("Refreshing access token - ShopId: {ShopId}", shopId);

        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("No refresh token available - ShopId: {ShopId}", shopId);
                throw new InvalidOperationException("No refresh token available");
            }

            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/auth/access_token/get";
            
            var sign = ShopeeApiHelper.GenerateSign(_partnerId, _partnerKey, path, timestamp);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}";

            // Body contém refresh_token, shop_id e partner_id
            var body = new
            {
                refresh_token = refreshToken,
                shop_id = shopId,
                partner_id = int.Parse(_partnerId)
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Refreshing access token - ShopId: {ShopId}, Timestamp: {Timestamp}", shopId, timestamp);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Token refresh response - ShopId: {ShopId}, StatusCode: {StatusCode}, Content: {Content}",
                shopId, response.StatusCode, responseContent);

            // Verificar status code
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed - ShopId: {ShopId}, StatusCode: {StatusCode}, Error: {Error}",
                    shopId, response.StatusCode, responseContent);
                throw new InvalidOperationException($"Refresh failed: {response.StatusCode} {responseContent}");
            }

            // Parse response
            var responseJson = JsonDocument.Parse(responseContent);
            var rootElement = responseJson.RootElement;

            // Verificar se há erro na resposta
            if (rootElement.TryGetProperty("error", out var errorElement))
            {
                var errorValue = errorElement.GetString();
                if (!string.IsNullOrEmpty(errorValue))
                {
                    _logger.LogWarning("Shopee error on token refresh - ShopId: {ShopId}, Error: {Error}, Response: {Response}",
                        shopId, errorValue, responseContent);
                    throw new InvalidOperationException($"Refresh error: {responseContent}");
                }
            }

            // Extrair tokens
            var accessToken = ShopeeApiHelper.GetJsonProperty(responseJson, "access_token");
            var newRefreshToken = ShopeeApiHelper.GetJsonProperty(responseJson, "refresh_token");
            
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("No access token in refresh response - ShopId: {ShopId}", shopId);
                throw new InvalidOperationException("No access token in refresh response");
            }

            // Parse expires_in (pode vir como "expires_in" ou "expire_in")
            long expiresIn = 3600; // default 1 hora
            if (rootElement.TryGetProperty("expires_in", out var expiresInElement))
            {
                if (expiresInElement.ValueKind == JsonValueKind.Number && 
                    expiresInElement.TryGetInt64(out var expiresInValue))
                {
                    expiresIn = expiresInValue;
                }
                else if (expiresInElement.ValueKind == JsonValueKind.String &&
                         long.TryParse(expiresInElement.GetString(), out var expiresInString))
                {
                    expiresIn = expiresInString;
                }
            }
            else if (rootElement.TryGetProperty("expire_in", out var expireInElement))
            {
                if (expireInElement.ValueKind == JsonValueKind.Number && 
                    expireInElement.TryGetInt64(out var expireInValue))
                {
                    expiresIn = expireInValue;
                }
                else if (expireInElement.ValueKind == JsonValueKind.String &&
                         long.TryParse(expireInElement.GetString(), out var expireInString))
                {
                    expiresIn = expireInString;
                }
            }

            _logger.LogInformation("Token refreshed successfully - ShopId: {ShopId}, ExpiresIn: {ExpiresIn}s",
                shopId, expiresIn);

            // Calcular expira_at
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiresAt = now + expiresIn;

            // Atualizar cache com os novos tokens
            await _cacheService.SaveManyAsync(
                ($"{shopId}_access_token", accessToken),
                ($"{shopId}_refresh_token", newRefreshToken),
                ($"{shopId}_access_token_expires_at", expiresAt.ToString())
            );

            _logger.LogDebug("Tokens cached - ShopId: {ShopId}, ExpiresAt: {ExpiresAt}", shopId, expiresAt);

            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Obtém informações detalhadas da loja Shopee
    /// Endpoint: GET /api/v2/shop/get_shop_info
    /// Obtém o access_token automaticamente do cache
    /// Ref: https://open.shopee.com/documents/v2/v2.shop.get_shop_info
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <returns>Objeto ShopeeShopInfoResponse com informações da loja</returns>
    public async Task<ShopeeShopInfoResponse> GetShopInfoAsync(long shopId)
    {
        _logger.LogInformation("Getting shop info - ShopId: {ShopId}", shopId);

        try
        {
            // Obter token do cache automaticamente
            var accessToken = await GetCachedAccessTokenAsync(shopId);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError("Failed to obtain access token from cache - ShopId: {ShopId}", shopId);
                throw new InvalidOperationException("Access token is required");
            }

            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/shop/get_shop_info";
            
            // Para shop-level APIs, a assinatura inclui access_token e shop_id
            // Base string: {partner_id}{path}{timestamp}{access_token}{shop_id}
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            // URL da API com parâmetros de autenticação
            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}";

            _logger.LogDebug("GetShopInfo URL - ShopId: {ShopId}, Path: {Path}, Timestamp: {Timestamp}", shopId, path, timestamp);

            var response = await _httpClient.GetAsync(url);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("GetShopInfo Response - ShopId: {ShopId}, StatusCode: {StatusCode}, Content: {Content}", 
                shopId, response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Shopee API error - ShopId: {ShopId}, StatusCode: {StatusCode}, Error: {Error}",
                    shopId, response.StatusCode, responseContent);
                throw new InvalidOperationException($"Failed to get shop info: {response.StatusCode} - {responseContent}");
            }
            
            // Deserializar para ShopeeShopInfoResponse
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var shopInfo = JsonSerializer.Deserialize<ShopeeShopInfoResponse>(responseContent, options);

            if (shopInfo == null)
            {
                _logger.LogError("Failed to deserialize shop info response - ShopId: {ShopId}", shopId);
                throw new InvalidOperationException("Failed to deserialize shop info response");
            }

            // Verificar se há erro na resposta
            if (!string.IsNullOrEmpty(shopInfo.Error))
            {
                _logger.LogWarning("Shopee error response - ShopId: {ShopId}, Error: {Error}, Message: {Message}",
                    shopId, shopInfo.Error, shopInfo.Message);
                throw new InvalidOperationException($"Error getting shop info: {shopInfo.Message}");
            }

            _logger.LogInformation("Shop info obtained successfully - ShopId: {ShopId}, ShopName: {ShopName}",
                shopId, shopInfo.ShopName);

            return shopInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shop info - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    #region Product Methods

    /// <summary>
    /// Obtém lista de categorias disponíveis na Shopee
    /// Endpoint: GET /api/v2/product/get_category
    /// Ref: https://open.shopee.com/documents/v2/v2.product.get_category
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="language">Idioma das categorias (ex: pt, en, zh-Hans)</param>
    /// <returns>JSON response com lista de categorias</returns>
    public async Task<JsonDocument> GetCategoryListAsync(long shopId, string language = "pt")
    {
        _logger.LogInformation("Getting category list - ShopId: {ShopId}, Language: {Language}", shopId, language);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/get_category";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}&language={language}";

            _logger.LogDebug("GetCategoryList URL - ShopId: {ShopId}", shopId);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("GetCategoryList Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get category list: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category list - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Obtém lista de itens/produtos da loja
    /// Endpoint: GET /api/v2/product/get_item_list
    /// Ref: https://open.shopee.com/documents/v2/v2.product.get_item_list
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="offset">Offset para paginação (default: 0)</param>
    /// <param name="pageSize">Tamanho da página (default: 20, max: 100)</param>
    /// <param name="itemStatus">Status dos itens: NORMAL, BANNED, DELETED, UNLIST (default: NORMAL)</param>
    /// <returns>JSON response com lista de itens</returns>
    public async Task<JsonDocument> GetItemListAsync(long shopId, int offset = 0, int pageSize = 20, string itemStatus = "NORMAL")
    {
        _logger.LogInformation("Getting item list - ShopId: {ShopId}, Offset: {Offset}, PageSize: {PageSize}, Status: {Status}",
            shopId, offset, pageSize, itemStatus);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/get_item_list";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}&offset={offset}&page_size={pageSize}&item_status={itemStatus}";

            _logger.LogDebug("GetItemList URL - ShopId: {ShopId}", shopId);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("GetItemList Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get item list: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item list - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Obtém informações básicas de um ou mais itens
    /// Endpoint: GET /api/v2/product/get_item_base_info
    /// Ref: https://open.shopee.com/documents/v2/v2.product.get_item_base_info
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemIds">Lista de IDs dos itens (max: 50)</param>
    /// <returns>JSON response com informações dos itens</returns>
    public async Task<JsonDocument> GetItemBaseInfoAsync(long shopId, params long[] itemIds)
    {
        _logger.LogInformation("Getting item base info - ShopId: {ShopId}, ItemIds: {ItemIds}",
            shopId, string.Join(",", itemIds));

        try
        {
            if (itemIds.Length == 0 || itemIds.Length > 50)
            {
                throw new ArgumentException("Item IDs must be between 1 and 50 items");
            }

            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/get_item_base_info";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var itemIdList = string.Join(",", itemIds);
            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}&item_id_list={itemIdList}";

            _logger.LogDebug("GetItemBaseInfo URL - ShopId: {ShopId}", shopId);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("GetItemBaseInfo Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get item base info: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item base info - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Adiciona um novo item/produto à loja
    /// Endpoint: POST /api/v2/product/add_item
    /// Ref: https://open.shopee.com/documents/v2/v2.product.add_item
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemData">Dados do item em formato de objeto (será serializado para JSON)</param>
    /// <returns>JSON response com dados do item criado</returns>
    public async Task<JsonDocument> AddItemAsync(long shopId, object itemData)
    {
        _logger.LogInformation("Adding item - ShopId: {ShopId}", shopId);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/add_item";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}";

            var content = new StringContent(
                JsonSerializer.Serialize(itemData),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("AddItem URL - ShopId: {ShopId}, Body: {Body}", shopId, JsonSerializer.Serialize(itemData));

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("AddItem Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to add item: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza um item/produto existente
    /// Endpoint: POST /api/v2/product/update_item
    /// Ref: https://open.shopee.com/documents/v2/v2.product.update_item
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item a ser atualizado</param>
    /// <param name="itemData">Dados do item para atualização</param>
    /// <returns>JSON response com dados do item atualizado</returns>
    public async Task<JsonDocument> UpdateItemAsync(long shopId, long itemId, object itemData)
    {
        _logger.LogInformation("Updating item - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/update_item";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}";

            // Adiciona item_id ao objeto de dados
            var updateData = new Dictionary<string, object>
            {
                ["item_id"] = itemId
            };

            // Mescla itemData com updateData
            var itemDataJson = JsonSerializer.Serialize(itemData);
            var itemDataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(itemDataJson);
            if (itemDataDict != null)
            {
                foreach (var kvp in itemDataDict)
                {
                    updateData[kvp.Key] = kvp.Value;
                }
            }

            var content = new StringContent(
                JsonSerializer.Serialize(updateData),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("UpdateItem URL - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("UpdateItem Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to update item: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            throw;
        }
    }

    #endregion

    #region Model/Variation Methods

    /// <summary>
    /// Inicializa variações de tier para um item existente
    /// Endpoint: POST /api/v2/product/init_tier_variation
    /// Ref: https://open.shopee.com/documents/v2/v2.product.init_tier_variation
    /// Use este método para adicionar variações a um item que foi criado sem variações
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="standardiseTierVariation">Lista de variações padronizadas (ex: Color, Size)</param>
    /// <param name="model">Lista de modelos para cada combinação de variações</param>
    /// <returns>JSON response com dados das variações criadas</returns>
    public async Task<JsonDocument> InitTierVariationAsync(long shopId, long itemId, object standardiseTierVariation, object model)
    {
        _logger.LogInformation("Initializing tier variation - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/init_tier_variation";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}";

            var requestData = new Dictionary<string, object>
            {
                ["item_id"] = itemId,
                ["standardise_tier_variation"] = standardiseTierVariation,
                ["model"] = model
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("InitTierVariation URL - ShopId: {ShopId}, ItemId: {ItemId}, Body: {Body}", 
                shopId, itemId, JsonSerializer.Serialize(requestData));

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("InitTierVariation Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to init tier variation: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing tier variation - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            throw;
        }
    }

    /// <summary>
    /// Obtém lista de modelos/variações de um item
    /// Endpoint: GET /api/v2/product/get_model_list
    /// Ref: https://open.shopee.com/documents/v2/v2.product.get_model_list
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <returns>JSON response com lista de modelos</returns>
    public async Task<JsonDocument> GetModelListAsync(long shopId, long itemId)
    {
        _logger.LogInformation("Getting model list - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/get_model_list";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}&item_id={itemId}";

            _logger.LogDebug("GetModelList URL - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("GetModelList Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get model list: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model list - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            throw;
        }
    }

    /// <summary>
    /// Adiciona modelos/variações a um item existente
    /// Endpoint: POST /api/v2/product/add_model
    /// Ref: https://open.shopee.com/documents/v2/v2.product.add_model
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="modelData">Dados dos modelos a serem adicionados</param>
    /// <returns>JSON response com dados dos modelos criados</returns>
    public async Task<JsonDocument> AddModelAsync(long shopId, long itemId, object modelData)
    {
        _logger.LogInformation("Adding model - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/add_model";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}";

            // Adiciona item_id ao objeto de dados
            var addData = new Dictionary<string, object>
            {
                ["item_id"] = itemId
            };

            // Mescla modelData com addData
            var modelDataJson = JsonSerializer.Serialize(modelData);
            var modelDataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(modelDataJson);
            if (modelDataDict != null)
            {
                foreach (var kvp in modelDataDict)
                {
                    addData[kvp.Key] = kvp.Value;
                }
            }

            var content = new StringContent(
                JsonSerializer.Serialize(addData),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("AddModel URL - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("AddModel Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to add model: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding model - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza modelos/variações de um item
    /// Endpoint: POST /api/v2/product/update_model
    /// Ref: https://open.shopee.com/documents/v2/v2.product.update_model
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="modelData">Dados dos modelos para atualização</param>
    /// <returns>JSON response com dados dos modelos atualizados</returns>
    public async Task<JsonDocument> UpdateModelAsync(long shopId, long itemId, object modelData)
    {
        _logger.LogInformation("Updating model - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/product/update_model";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}";

            // Adiciona item_id ao objeto de dados
            var updateData = new Dictionary<string, object>
            {
                ["item_id"] = itemId
            };

            // Mescla modelData com updateData
            var modelDataJson = JsonSerializer.Serialize(modelData);
            var modelDataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(modelDataJson);
            if (modelDataDict != null)
            {
                foreach (var kvp in modelDataDict)
                {
                    updateData[kvp.Key] = kvp.Value;
                }
            }

            var content = new StringContent(
                JsonSerializer.Serialize(updateData),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("UpdateModel URL - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("UpdateModel Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to update model: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            throw;
        }
    }

    #endregion

    #region Order Methods

    /// <summary>
    /// Obtém lista de pedidos da loja
    /// Endpoint: GET /api/v2/order/get_order_list
    /// Ref: https://open.shopee.com/documents/v2/v2.order.get_order_list
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="timeRangeField">Campo de tempo para filtro: create_time, update_time</param>
    /// <param name="timeFrom">Timestamp inicial do período</param>
    /// <param name="timeTo">Timestamp final do período</param>
    /// <param name="pageSize">Tamanho da página (default: 20, max: 100)</param>
    /// <param name="cursor">Cursor para paginação</param>
    /// <param name="orderStatus">Status do pedido: UNPAID, READY_TO_SHIP, PROCESSED, SHIPPED, COMPLETED, IN_CANCEL, CANCELLED, INVOICE_PENDING</param>
    /// <returns>JSON response com lista de pedidos</returns>
    public async Task<JsonDocument> GetOrderListAsync(
        long shopId,
        string timeRangeField,
        long timeFrom,
        long timeTo,
        int pageSize = 20,
        string? cursor = null,
        string? orderStatus = null)
    {
        _logger.LogInformation("Getting order list - ShopId: {ShopId}, TimeRangeField: {TimeRangeField}, TimeFrom: {TimeFrom}, TimeTo: {TimeTo}",
            shopId, timeRangeField, timeFrom, timeTo);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/order/get_order_list";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}&time_range_field={timeRangeField}&time_from={timeFrom}&time_to={timeTo}&page_size={pageSize}";

            if (!string.IsNullOrEmpty(cursor))
            {
                url += $"&cursor={cursor}";
            }

            if (!string.IsNullOrEmpty(orderStatus))
            {
                url += $"&order_status={orderStatus}";
            }

            _logger.LogDebug("GetOrderList URL - ShopId: {ShopId}", shopId);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("GetOrderList Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get order list: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order list - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Obtém detalhes de um ou mais pedidos
    /// Endpoint: GET /api/v2/order/get_order_detail
    /// Ref: https://open.shopee.com/documents/v2/v2.order.get_order_detail
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="orderSnList">Lista de order_sn (números dos pedidos)</param>
    /// <param name="responseOptionalFields">Campos opcionais a serem retornados (ex: buyer_user_id, buyer_username, etc)</param>
    /// <returns>JSON response com detalhes dos pedidos</returns>
    public async Task<JsonDocument> GetOrderDetailAsync(
        long shopId,
        string[] orderSnList)
    {
        _logger.LogInformation("Getting order detail - ShopId: {ShopId}, OrderSnList: {OrderSnList}",
            shopId, string.Join(",", orderSnList));

        try
        {
            if (orderSnList.Length == 0 || orderSnList.Length > 50)
            {
                throw new ArgumentException("Order SN list must be between 1 and 50 items");
            }

            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/order/get_order_detail";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var orderSnListParam = string.Join(",", orderSnList);
            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}&order_sn_list={orderSnListParam}";

            const string responseOptionalFields = "buyer_user_id,buyer_username,estimated_shipping_fee,recipient_address,actual_shipping_fee ,goods_to_declare,note,note_update_time,item_list,pay_time,dropshipper, dropshipper_phone,split_up,buyer_cancel_reason,cancel_by,cancel_reason,actual_shipping_fee_confirmed,buyer_cpf_id,fulfillment_flag,pickup_done_time,package_list,shipping_carrier,payment_method,total_amount,buyer_username,invoice_data,order_chargeable_weight_gram,return_request_due_date,edt,payment_info";
            
            if (responseOptionalFields != null && responseOptionalFields.Length > 0)
            {
                url += $"&response_optional_fields={responseOptionalFields}";
            }

            _logger.LogDebug("GetOrderDetail URL - ShopId: {ShopId}", shopId);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("GetOrderDetail Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get order detail: {response.StatusCode} - {responseContent}");
            }

            return JsonDocument.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order detail - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    #endregion

    #region Media

    /// <summary>
    /// Faz upload de uma imagem para a Shopee
    /// Endpoint: POST /api/v2/media_space/upload_image
    /// Ref: https://open.shopee.com/documents/v2/v2.media_space.upload_image
    /// 
    /// A resposta contém um image_id que pode ser usado em operações de produtos
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="imageStream">Stream do arquivo de imagem para upload</param>
    /// <param name="fileName">Nome do arquivo (opcional, usado para logs)</param>
    /// <returns>JSON response com image_id e outras informações da imagem uploaded</returns>
    public async Task<JsonDocument> UploadImageAsync(long shopId, Stream imageStream, string fileName = "image.jpg")
    {
        _logger.LogInformation("Uploading image - ShopId: {ShopId}, FileName: {FileName}", shopId, fileName);

        try
        {
            var accessToken = await GetCachedAccessTokenAsync(shopId);
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            const string path = "/api/v2/media_space/upload_image";
            var sign = ShopeeApiHelper.GenerateSignWithShop(_partnerId, _partnerKey, path, timestamp, accessToken, shopId);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&access_token={accessToken}&shop_id={shopId}&sign={sign}";

            // Usar MultipartFormDataContent para enviar o arquivo como form data
            using var form = new MultipartFormDataContent();
            
            // Adicionar o arquivo de imagem
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(imageContent, "image", fileName);

            _logger.LogDebug("UploadImage URL - ShopId: {ShopId}, FileName: {FileName}", shopId, fileName);

            var response = await _httpClient.PostAsync(url, form);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("UploadImage Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to upload image: {response.StatusCode} - {responseContent}");
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            
            // Extrair image_id se disponível
            if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.TryGetProperty("image_id", out var imageIdElement))
                {
                    var imageId = imageIdElement.GetString();
                    _logger.LogInformation("Image uploaded successfully - ShopId: {ShopId}, ImageId: {ImageId}", shopId, imageId);
                }
            }

            return jsonResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image - ShopId: {ShopId}, FileName: {FileName}", shopId, fileName);
            throw;
        }
    }

    #endregion
}
