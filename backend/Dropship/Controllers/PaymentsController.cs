using Microsoft.AspNetCore.Mvc;
using Dropship.Services;

namespace Dropship.Controllers;

[ApiController]
[Route("[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var id = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("[PAYMENTS] Unauthorized access - missing user ID claim");
            return Unauthorized();
        }

        _logger.LogInformation("[PAYMENTS] Fetching pending payments for seller: {SellerId}", id);
        
        var supplierPayments = await _paymentService.GetPendingPaymentsAsync(id);
        
        _logger.LogInformation("[PAYMENTS] Retrieved {Count} suppliers with pending payments for seller: {SellerId}", supplierPayments.Count, id);
        return Ok(supplierPayments);
    }
}