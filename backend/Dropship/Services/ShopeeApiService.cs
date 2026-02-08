using System.Security.Cryptography;
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
    
    // Path para autenticação legacy
    private const string AuthPartnerLegacyPath = "/api/v2/shop/auth_partner";
    
    // Paths para chamadas de API (após ter o token)
    private const string GetTokenPath = "/api/v2/auth/token/get";
    private const string GetShopInfoPath = "/api/v2/shop/get_shop_info";

    public ShopeeApiService(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger<ShopeeApiService> logger)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _logger = logger;

        // TODO: Obter credenciais de IConfiguration ou AWS Secrets Manager
        _partnerId = Environment.GetEnvironmentVariable("SHOPEE_PARTNER_ID") ?? "1203628";
        _partnerKey = "shpk4871546d53586b746b4c57614a4b5a577a4476726a4e6747765749665468";
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

            var path = AuthPartnerLegacyPath; // "/api/v2/shop/auth_partner"
            
            var timest = ShopeeApiHelper.GetCurrentTimestamp();
            
            // Construir redirect URI dinâmica com o email
            var redirectUrl = $"{requestUri}/sellers/{Uri.EscapeDataString(email)}/store/code";
            
            // Base string para legacy API: {partner_id}{path}{timestamp}
            var tmpBaseString = $"{_partnerId}{path}{timest}";
            var baseString = Encoding.UTF8.GetBytes(tmpBaseString);
            var partnerKey = Encoding.UTF8.GetBytes(_partnerKey);
            
            _logger.LogDebug("HMAC Input - PartnerId: {PartnerId}, Path: {Path}, Timestamp: {Timestamp}, BaseString: {BaseString}", _partnerId, path, timest, tmpBaseString);
            _logger.LogDebug("HMAC PartnerKey length: {KeyLength} bytes", partnerKey.Length);
            
            // Calcular HMAC SHA256
            using var hmac = new HMACSHA256(partnerKey);
            var hashBytes = hmac.ComputeHash(baseString);
            var sign = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                
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
            var path = GetTokenPath;
            var sign = GenerateSign(path, timestamp);

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

            // Nenhum token válido: fazer troca completa de token
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogError("No valid token in cache and no authorization code provided - ShopId: {ShopId}", shopId);
                throw new InvalidOperationException("Authorization code is required when no valid token is cached");
            }

            _logger.LogInformation("Obtaining new access token via full exchange - ShopId: {ShopId}", shopId);
            var (newToken, newRefreshToken, expiresIn) = await GetTokenShopLevelAsync(code, shopId);

            if (string.IsNullOrEmpty(newToken))
            {
                _logger.LogError("Failed to obtain new access token - ShopId: {ShopId}", shopId);
                throw new InvalidOperationException("Failed to obtain access token");
            }

            // Cache dos novos tokens
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached access token - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Faz refresh do access token usando o refresh token
    /// </summary>
    private async Task<string> RefreshAccessTokenAsync(long shopId, string refreshToken)
    {
        _logger.LogInformation("Refreshing access token - ShopId: {ShopId}", shopId);

        try
        {
            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            var path = "/api/v2/auth/token/refresh"; 
            var sign = GenerateSign(path, timestamp);

            var url = $"{DefaultApiHost}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}";

            var body = new
            {
                refresh_token = refreshToken,
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
                _logger.LogWarning("Token refresh failed - ShopId: {ShopId}, StatusCode: {StatusCode}, Error: {Error}",
                    shopId, response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to refresh token: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            if (responseJson.RootElement.TryGetProperty("error", out var errorElement) && !string.IsNullOrEmpty(errorElement.GetString()))
            {
                var errorMessage = responseJson.RootElement.TryGetProperty("message", out var msgElement)
                    ? msgElement.GetString()
                    : "Unknown error";

                _logger.LogWarning("Shopee error on token refresh - ShopId: {ShopId}, Error: {Error}, Message: {Message}",
                    shopId, errorElement.GetString(), errorMessage);
                throw new InvalidOperationException($"Error refreshing token: {errorMessage}");
            }

            var accessToken = ShopeeApiHelper.GetJsonProperty(responseJson, "access_token");
            var newRefreshToken = ShopeeApiHelper.GetJsonProperty(responseJson, "refresh_token");
            var expiresIn = ShopeeApiHelper.ParseExpiresIn(responseJson);

            _logger.LogInformation("Token refreshed successfully - ShopId: {ShopId}, ExpiresIn: {ExpiresIn}",
                shopId, expiresIn);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiresAt = now + expiresIn;

            await _cacheService.SaveManyAsync(
                ($"{shopId}_access_token", accessToken),
                ($"{shopId}_refresh_token", newRefreshToken),
                ($"{shopId}_access_token_expires_at", expiresAt.ToString())
            );

            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token - ShopId: {ShopId}", shopId);
            throw;
        }
    }

    /// <summary>
    /// Gera assinatura HMAC SHA256 para requisições à API (public endpoints)
    /// Base string: {partner_id}{path}{timestamp}
    /// </summary>
    private string GenerateSign(string path, long timestamp)
    {
        try
        {
            var tmpBaseString = $"{_partnerId}{path}{timestamp}";
            var baseString = Encoding.UTF8.GetBytes(tmpBaseString);
            var key = Encoding.UTF8.GetBytes(_partnerKey);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(baseString);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HMAC sign");
            throw;
        }
    }

    /// <summary>
    /// Gera assinatura HMAC SHA256 para requisições à API (shop-level endpoints)
    /// Base string: {partner_id}{path}{timestamp}{access_token}{shop_id}
    /// Ref: https://open.shopee.com/documents/v2/v2.shop.get_shop_info
    /// </summary>
    private string GenerateSignWithShop(string path, long timestamp, string accessToken, long shopId)
    {
        try
        {
            var tmpBaseString = $"{_partnerId}{path}{timestamp}{accessToken}{shopId}";
            _logger.LogDebug("GenerateSignWithShop - BaseString: {BaseString}", tmpBaseString);
            
            var baseString = Encoding.UTF8.GetBytes(tmpBaseString);
            var key = Encoding.UTF8.GetBytes(_partnerKey);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(baseString);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HMAC sign with shop");
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
            var path = GetShopInfoPath;
            
            // Para shop-level APIs, a assinatura inclui access_token e shop_id
            // Base string: {partner_id}{path}{timestamp}{access_token}{shop_id}
            var sign = GenerateSignWithShop(path, timestamp, accessToken, shopId);

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
}
