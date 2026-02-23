using Microsoft.AspNetCore.Mvc;
using Dropship.Services;
using Dropship.Requests;

namespace Dropship.Controllers;

/// <summary>
/// Controller para processar webhooks do InfinityPay
/// Recebe notificações de pagamentos e atualiza o status
/// </summary>
[ApiController]
[Route("webhook")]
public class InfinityPayWebhookController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<InfinityPayWebhookController> _logger;

    public InfinityPayWebhookController(
        PaymentService paymentService,
        ILogger<InfinityPayWebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

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
            _logger.LogInformation(
                "Received InfinityPay webhook - InvoiceSlug: {InvoiceSlug}, Amount: {Amount}, OrderNsu: {OrderNsu}",
                request.InvoiceSlug, request.PaidAmount, request.OrderNsu);

            // Validar campos obrigatórios
            if (string.IsNullOrWhiteSpace(request.InvoiceSlug))
            {
                _logger.LogWarning("InfinityPay webhook missing invoice_slug");
                return BadRequest(new { error = "invoice_slug is required" });
            }

            if (string.IsNullOrWhiteSpace(request.OrderNsu))
            {
                _logger.LogWarning("InfinityPay webhook missing order_nsu");
                return BadRequest(new { error = "order_nsu is required" });
            }

            _logger.LogInformation(
                "Processing InfinityPay payment - InvoiceSlug: {InvoiceSlug}, Amount: {Amount}, OrderNsu: {OrderNsu}",
                request.InvoiceSlug, request.PaidAmount, request.OrderNsu);

            // Processar o pagamento usando o link
            // O orderNsu aqui é o webhookOrderNsu que contém os paymentIds
            await _paymentService.ProcessPaymentWebhookWithLinkAsync(
                webhookOrderNsu: request.OrderNsu,
                paidAmount: request.PaidAmount,
                installments: request.Installments,
                transactionNsu: request.TransactionNsu,
                orderNsu: request.OrderNsu,
                captureMethod: request.CaptureMethod,
                receiptUrl: request.ReceiptUrl);

            _logger.LogInformation(
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
            _logger.LogError(ex, "Invalid operation in InfinityPay webhook");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing InfinityPay webhook");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}





