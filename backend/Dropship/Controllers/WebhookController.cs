using Microsoft.AspNetCore.Mvc;
using Dropship.Services;
using Dropship.Requests;
using Dropship.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Dropship.Controllers;

/// <summary>
/// Controller para processar webhooks do InfinityPay
/// Recebe notificações de pagamentos e atualiza o status
/// </summary>
[ApiController]
[Route("webhook")]
public class WebhookController(
    PaymentService paymentService,
    ShopeeService shopeeService,
    ILogger<WebhookController> logger)
    : ControllerBase
{
    private const int EventCodeNewOrderReceived = 3;    
    /// <summary>
    /// Webhook para notificação de pagamento do InfinityPay
    /// Formato orderNsu esperado: paymentId1-paymentId2-...-paymentIdN
    /// Suporta múltiplos pagamentos em um único webhook
    /// Exemplo: abc123def456-xyz789uvw012-qrs345tuvwxy
    /// </summary>
    [HttpPost("infinitypay/payment")]
    public async Task<IActionResult> ProcessPaymentWebhook([FromBody] InfinityPayWebhookRequest request)
    {
        try
        {
            logger.LogInformation(
                "Received InfinityPay webhook - InvoiceSlug: {InvoiceSlug}, Amount: {Amount}, OrderNsu: {OrderNsu}",
                request.InvoiceSlug, request.PaidAmount, request.OrderNsu);

            // Validar campos obrigatórios
            if (string.IsNullOrWhiteSpace(request.InvoiceSlug))
            {
                logger.LogWarning("InfinityPay webhook missing invoice_slug");
                return BadRequest(new { error = "invoice_slug is required" });
            }

            if (string.IsNullOrWhiteSpace(request.OrderNsu))
            {
                logger.LogWarning("InfinityPay webhook missing order_nsu");
                return BadRequest(new { error = "order_nsu is required" });
            }

            logger.LogInformation(
                "Processing InfinityPay payment - InvoiceSlug: {InvoiceSlug}, Amount: {Amount}, OrderNsu: {OrderNsu}",
                request.InvoiceSlug, request.PaidAmount, request.OrderNsu);

            // Processar o pagamento usando o link
            // O orderNsu aqui é o webhookOrderNsu que contém os paymentIds
            await paymentService.ProcessPaymentWebhookWithLinkAsync(
                linkId: request.OrderNsu,
                paidAmount: request.PaidAmount,
                installments: request.Installments,
                transactionNsu: request.TransactionNsu,
                captureMethod: request.CaptureMethod,
                receiptUrl: request.ReceiptUrl);

            logger.LogInformation(
                "InfinityPay payment processed successfully - InvoiceSlug: {InvoiceSlug}",
                request.InvoiceSlug);

            return Ok(new
            {
                statusCode = 200,
                message = "Payment processed successfully",
                invoiceSlug = request.InvoiceSlug,
                orderNsu = request.OrderNsu
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid operation in InfinityPay webhook");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing InfinityPay webhook");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
    
     /// <summary>
    /// Webhook para receber eventos do Shopee
    /// </summary>
    /// <param name="request">Payload do webhook do Shopee</param>
    /// <returns>Resposta do webhook</returns>
    [HttpPost("shopee")]
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





