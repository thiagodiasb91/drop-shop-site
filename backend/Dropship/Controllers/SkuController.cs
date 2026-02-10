using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciar SKUs (Stock Keeping Units)
/// </summary>
[ApiController]
[Route("products/{productId}/skus")]
public class SkuController : ControllerBase
{
    private readonly SkuRepository _skuRepository;
    private readonly ILogger<SkuController> _logger;

    public SkuController(SkuRepository skuRepository, ILogger<SkuController> logger)
    {
        _skuRepository = skuRepository;
        _logger = logger;
    }

   
}
