using Microsoft.AspNetCore.Mvc;
using Dropship.Services;
using Dropship.Responses;

namespace Dropship.Controllers;

/// <summary>
/// Controller de Interface Shopee para testes diretos de API
/// Expõe métodos do serviço ShopeeApiService
/// Útil para testar chamadas à API da Shopee sem necessidade de debug
/// </summary>
[ApiController]
[Route("shopee-interface")]
public class ShopeeInterfaceController : ControllerBase
{
    private readonly ShopeeApiService _shopeeApiService;
    private readonly ILogger<ShopeeInterfaceController> _logger;

    public ShopeeInterfaceController(
        ShopeeApiService shopeeApiService,
        ILogger<ShopeeInterfaceController> logger)
    {
        _shopeeApiService = shopeeApiService;
        _logger = logger;
    }

    /// <summary>
    /// Gera URL de autenticação com Shopee
    /// Útil para testes iniciais de auth flow
    /// </summary>
    /// <param name="email">Email do seller</param>
    /// <param name="requestUri">URI base da aplicação (ex: http://localhost:5000)</param>
    /// <returns>URL de autenticação para redirecionar o usuário ao Shopee</returns>
    [HttpGet("auth-url")]
    [ProducesResponseType(typeof(AuthUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAuthUrl([FromQuery] string email, [FromQuery] string requestUri)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetAuthUrl - Email: {Email}, RequestUri: {RequestUri}", email, requestUri);

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(requestUri))
            {
                _logger.LogWarning("[SHOPEE-TEST] Missing email or requestUri");
                return BadRequest(new { error = "Email and requestUri are required" });
            }

            var authUrl = _shopeeApiService.GetAuthUrl(email, requestUri);
            _logger.LogInformation("[SHOPEE-TEST] Auth URL generated successfully");

            return Ok(new AuthUrlResponse { AuthUrl = authUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error generating auth URL");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém token de loja (shop-level) usando authorization code
    /// Etapa 2 do fluxo OAuth2
    /// </summary>
    /// <param name="code">Authorization code retornado pelo Shopee</param>
    /// <param name="shopId">ID da loja no Shopee</param>
    /// <returns>Tokens de acesso e refresh</returns>
    [HttpPost("get-token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetToken([FromQuery] string code, [FromQuery] long shopId)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetToken - Code: {Code}, ShopId: {ShopId}", code, shopId);

        try
        {
            var accessToken = await _shopeeApiService.GetCachedAccessTokenAsync(shopId, code);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting token - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém informações detalhadas da loja
    /// O access token é obtido automaticamente do cache usando shopId
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <returns>Informações da loja Shopee</returns>
    [HttpGet("shop-info")]
    [ProducesResponseType(typeof(ShopeeShopInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetShopInfo([FromQuery] long shopId)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetShopInfo - ShopId: {ShopId}", shopId);

        try
        {
            if (shopId <= 0)
            {
                _logger.LogWarning("[SHOPEE-TEST] Invalid shopId");
                return BadRequest(new { error = "Valid shopId is required" });
            }

            var shopInfo = await _shopeeApiService.GetShopInfoAsync(shopId);

            _logger.LogInformation("[SHOPEE-TEST] Shop info obtained successfully - ShopId: {ShopId}",
                shopId);

            return Ok(shopInfo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[SHOPEE-TEST] Invalid operation - ShopId: {ShopId}", shopId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting shop info - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Response com URL de autenticação
/// </summary>
public class AuthUrlResponse
{
    public string AuthUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response com tokens de acesso
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long ExpiresIn { get; set; }
}

/// <summary>
/// Response com token de acesso do cache
/// </summary>
public class CachedTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
}

