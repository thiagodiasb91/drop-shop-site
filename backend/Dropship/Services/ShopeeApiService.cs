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
        ILogger<ShopeeApiService> logger)
    {
        _httpClient = httpClient;
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
    public async Task<(string AccessToken, string RefreshToken, long ExpiresIn)> GetTokenShopLevelAsync(string code, long shopId)
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
    /// Gera assinatura HMAC SHA256 para requisições à API
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
    /// Obtém informações detalhadas da loja Shopee
    /// Endpoint: GET /api/v2/shop/get_shop_info
    /// Requer: access_token válido
    /// </summary>
    /// <param name="accessToken">Token de acesso da loja</param>
    /// <param name="shopId">ID da loja</param>
    /// <returns>Objeto ShopeeShopInfoResponse com informações da loja</returns>
    public async Task<ShopeeShopInfoResponse> GetShopInfoAsync(string accessToken, long shopId)
    {
        _logger.LogInformation("Getting shop info - ShopId: {ShopId}", shopId);

        try
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError("Access token is empty or null");
                throw new InvalidOperationException("Access token is required");
            }

            var timestamp = ShopeeApiHelper.GetCurrentTimestamp();
            var path = GetShopInfoPath;
            var sign = GenerateSign(path, timestamp);

            // URL da API com parâmetros de autenticação
            var url = $"{DefaultApiHost}{path}?shop_id={shopId}&access_token={Uri.EscapeDataString(accessToken)}&timestamp={timestamp}&sign={sign}";

            _logger.LogDebug("GetShopInfo URL - ShopId: {ShopId}, Path: {Path}", shopId, path);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Shopee API error - ShopId: {ShopId}, StatusCode: {StatusCode}, Error: {Error}",
                    shopId, response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to get shop info: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Deserializar para ShopeeShopInfoResponse
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
