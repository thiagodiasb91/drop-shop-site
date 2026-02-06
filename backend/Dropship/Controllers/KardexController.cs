using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;

namespace Dropship.Controllers;

[ApiController]
[Route("kardex")]
public class KardexController : ControllerBase
{
    private readonly KardexRepository _kardexRepository;
    private readonly ILogger<KardexController> _logger;

    public KardexController(KardexRepository kardexRepository, ILogger<KardexController> logger)
    {
        _kardexRepository = kardexRepository;
        _logger = logger;
    }

    [HttpGet("{sku}")]
    public async Task<IActionResult> GetKardexBySku(string sku)
    {
        _logger.LogInformation("[KARDEX] Fetching kardex entries for SKU: {Sku}", sku);
        
        var kardexList = await _kardexRepository.GetKardexBySkuAsync(sku);
        
        _logger.LogInformation("[KARDEX] Retrieved {Count} kardex entries for SKU: {Sku}", kardexList.Count, sku);
        return Ok(kardexList);
    }
}