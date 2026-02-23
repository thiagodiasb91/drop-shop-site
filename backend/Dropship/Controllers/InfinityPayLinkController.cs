using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dropship.Requests;
using Dropship.Services;
using Dropship.Repository;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciar links de pagamento InfinityPay
/// </summary>
[ApiController]
[Route("payments/infinitypay")]
public class InfinityPayLinkController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly InfinityPayLinkRepository _linkRepository;
    private readonly ILogger<InfinityPayLinkController> _logger;

    public InfinityPayLinkController(
        PaymentService paymentService,
        InfinityPayLinkRepository linkRepository,
        ILogger<InfinityPayLinkController> logger)
    {
        _paymentService = paymentService;
        _linkRepository = linkRepository;
        _logger = logger;
    }

    /// <summary>
    /// Cria um link de pagamento para InfinityPay
    /// </summary>
    /// <param name="sellerId">ID do vendedor (obtido da claim)</param>
    /// <param name="request">Array de paymentIds e valor total</param>
    /// <returns>LinkId e URL de checkout</returns>
    [HttpPost("create-link")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentLink(
        [FromHeader(Name = "X-Seller-Id")] string sellerId,
        [FromBody] CreateInfinityPayLinkRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creating InfinityPay link - SellerId: {SellerId}, PaymentIds: {Count}",
                sellerId, request.PaymentIds.Count);

            // Validar campos obrigatórios
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                _logger.LogWarning("Missing seller ID in request");
                return BadRequest(new { error = "Seller ID is required (X-Seller-Id header)" });
            }

            if (request.PaymentIds == null || request.PaymentIds.Count == 0)
            {
                _logger.LogWarning("No payment IDs provided - SellerId: {SellerId}", sellerId);
                return BadRequest(new { error = "At least one payment ID is required" });
            }

            if (request.Amount <= 0)
            {
                _logger.LogWarning("Invalid amount - SellerId: {SellerId}", sellerId);
                return BadRequest(new { error = "Amount must be greater than 0" });
            }

            // Criar o link no serviço (valida se pagamentos existem e estão pending)
            var link = await _paymentService.CreateInfinityPayLinkAsync(
                sellerId: sellerId,
                paymentIds: request.PaymentIds,
                totalAmount: request.Amount);

            // Gerar URL de checkout
            // Formato: https://infinitepay.io/checkout?linkId=<ULID>&orderNsu=paymentId1-paymentId2
            var checkoutUrl = $"https://infinitepay.io/checkout?linkId={link.LinkId}&orderNsu={link.WebhookOrderNsu}";
            link.CheckoutUrl = checkoutUrl;

            // Persistir o link no banco
            await _linkRepository.CreateLinkAsync(link);

            _logger.LogInformation(
                "InfinityPay link created successfully - LinkId: {LinkId}, SellerId: {SellerId}",
                link.LinkId, sellerId);

            // Retornar response
            var response = new InfinityPayLinkResponse
            {
                LinkId = link.LinkId,
                CheckoutUrl = checkoutUrl,
                OrderNsu = link.WebhookOrderNsu ?? "",
                PaymentCount = link.PaymentCount,
                Amount = link.Amount,
                CreatedAt = link.CreatedAt
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Validation error creating InfinityPay link");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating InfinityPay link");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Obtém informações de um link de pagamento
    /// </summary>
    /// <param name="linkId">ID do link (ULID)</param>
    [HttpGet("link/{linkId}")]
    public async Task<IActionResult> GetLink(string linkId)
    {
        try
        {
            _logger.LogInformation("Getting InfinityPay link - LinkId: {LinkId}", linkId);

            var link = await _linkRepository.GetLinkByIdAsync(linkId);
            
            if (link == null)
            {
                _logger.LogWarning("Link not found - LinkId: {LinkId}", linkId);
                return NotFound(new { error = "Link not found" });
            }

            var response = new InfinityPayLinkResponse
            {
                LinkId = link.LinkId,
                CheckoutUrl = link.CheckoutUrl,
                OrderNsu = link.WebhookOrderNsu ?? "",
                PaymentCount = link.PaymentCount,
                Amount = link.Amount,
                CreatedAt = link.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting InfinityPay link - LinkId: {LinkId}", linkId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}





