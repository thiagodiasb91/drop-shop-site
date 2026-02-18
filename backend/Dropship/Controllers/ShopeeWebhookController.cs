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
public class ShopeeWebhookController(
    ShopeeService shopeeService,
    ShopeeApiService shopeeApiService,
    SellerRepository sellerRepository,
    ILogger<ShopeeWebhookController> logger)
    : ControllerBase
{
    private const int EventCodeNewOrderReceived = 3;

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

    [HttpGet("/sellers/{email}/store/code")]
    public async Task<IActionResult> MockResponseTest([FromRoute] string email, [FromQuery] string code, [FromQuery(Name = "shop_id")] long shopId)
    {
        return Ok(await shopeeApiService.GetCachedAccessTokenAsync(shopId, code));
    }
    
    /// <summary>
    /// Webhook para receber eventos do Shopee
    /// </summary>
    /// <param name="request">Payload do webhook do Shopee</param>
    /// <returns>Resposta do webhook</returns>
    [HttpPost("/webhook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShopeeWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ShopeeWebhookResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveWebhook([FromBody] ShopeeWebhookRequest request)
    {
        logger.LogInformation("Received Shopee webhook - Code: {Code}, ShopId: {ShopId}, MsgId: {MsgId}", 
            request.Code, request.ShopId, request.MsgId);

        try
        {
            // Verificação de heartbeat da própria Shopee (Code = 0)
            if (request.Code == 0)
            {
                logger.LogInformation("Shopee verification request received - MsgId: {MsgId}", request.MsgId);
                return Ok();
            }
            
            // Validar dados básicos
            if (string.IsNullOrWhiteSpace(request.MsgId))
            {
                logger.LogWarning("Invalid webhook - missing MsgId");
                return BadRequest(new ShopeeWebhookResponse
                {
                    StatusCode = 400,
                    Message = "MsgId is required"
                });
            }

            if (request.ShopId <= 0)
            {
                logger.LogWarning("Invalid webhook - invalid ShopId: {ShopId}", request.ShopId);
                return BadRequest(new ShopeeWebhookResponse
                {
                    StatusCode = 400,
                    Message = "ShopId must be greater than 0"
                });
            }

            if (request.Timestamp <= 0)
            {
                logger.LogWarning("Invalid webhook - invalid Timestamp: {Timestamp}", request.Timestamp);
                return BadRequest(new ShopeeWebhookResponse
                {
                    StatusCode = 400,
                    Message = "Timestamp is required"
                });
            }

            // Processar evento de nova ordem recebida
            if (request.Code == EventCodeNewOrderReceived)
            {
                logger.LogInformation("Processing new order event - MsgId: {MsgId}, ShopId: {ShopId}", 
                    request.MsgId, request.ShopId);

                // Validar dados da ordem
                if (request.Data == null || string.IsNullOrWhiteSpace(request.Data.OrderSn))
                {
                    logger.LogWarning("Invalid webhook - missing order data");
                    return BadRequest(new ShopeeWebhookResponse
                    {
                        StatusCode = 400,
                        Message = "Order data is required"
                    });
                }

                if (request.Data.UpdateTime <= 0)
                {
                    logger.LogWarning("Invalid webhook - invalid UpdateTime: {UpdateTime}", request.Data.UpdateTime);
                    return BadRequest(new ShopeeWebhookResponse
                    {
                        StatusCode = 400,
                        Message = "UpdateTime is required"
                    });
                }

                // Processar ordem
                await shopeeService.ProcessOrderReceivedAsync(
                    request.ShopId,
                    request.Data.OrderSn,
                    request.Data.Status
                );

                logger.LogInformation("New order accepted - OrderSn: {OrderSn}, MsgId: {MsgId}, Status: {Status}", 
                    request.Data.OrderSn, request.MsgId, request.Data.Status);

                return Ok(new ShopeeWebhookResponse
                {
                    StatusCode = 200,
                    Message = "New order accepted"
                });
            }
            else
            {
                logger.LogWarning("Unexpected event code: {Code}, MsgId: {MsgId}", request.Code, request.MsgId);
                return BadRequest(new ShopeeWebhookResponse
                {
                    StatusCode = 400,
                    Message = $"Event not expected {request.Code}"
                });
            }
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error - MsgId: {MsgId}", request.MsgId);
            return BadRequest(new ShopeeWebhookResponse
            {
                StatusCode = 400,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Shopee webhook - MsgId: {MsgId}, ShopId: {ShopId}", 
                request.MsgId, request.ShopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ShopeeWebhookResponse
            {
                StatusCode = 500,
                Message = "Internal server error"
            });
        }
    }
}
