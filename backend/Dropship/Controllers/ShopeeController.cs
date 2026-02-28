using Dropship.Repository;
using Microsoft.AspNetCore.Mvc;
using Dropship.Requests;
using Dropship.Responses;
using Dropship.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dropship.Controllers;

/// <summary>
/// Controller para webhooks do Shopee
/// </summary>
[ApiController]
[Route("shopee")]
public class ShopeeController(
    ShopeeApiService shopeeApiService,
    ILogger<ShopeeController> logger)
    : ControllerBase
{
    /// <summary>
    /// Gera a URL para autorização do Shopee
    /// Esta é a URL que deve ser fornecida ao cliente para autorizar a API
    /// A URL de redirect será configurada para: https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email={email}
    /// </summary>
    /// <param name="email">Email do usuário que será autorizado</param>
    /// <returns>URL de autorização com assinatura HMAC</returns>
    [HttpGet("auth-url")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAuthorizationUrl([FromQuery] string email)
    {
        var origin = Request.Headers.Origin;
        logger.LogInformation("Generating Shopee authorization URL - Email: {Email}", email);

        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Invalid request - email parameter is required");
            return BadRequest(new ShopeeWebhookResponse
            {
                StatusCode = 400,
                Message = "Email parameter is required"
            });
        }

        try
        {
            var authUrl = shopeeApiService.GetAuthUrl(email, origin);

            logger.LogInformation("Shopee authorization URL generated successfully - Email: {Email}", email);

            return Ok(new
            {
                statusCode = 200,
                message = "Authorization URL generated successfully",
                authUrl
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating Shopee authorization URL - Email: {Email}", email);
            return StatusCode(StatusCodes.Status500InternalServerError, new ShopeeWebhookResponse
            {
                StatusCode = 500,
                Message = "Error generating authorization URL"
            });
        }
    }

    [HttpGet("sellers/{email}/store/code")]
    public async Task<IActionResult> MockResponseTest([FromRoute] string email, [FromQuery] string code, [FromQuery(Name = "shop_id")] long shopId)
    {
        return Ok(await shopeeApiService.GetCachedAccessTokenAsync(shopId, code));
    }
    
}
