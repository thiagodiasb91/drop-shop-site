using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Domain;
using Dropship.Services;
using Dropship.Requests;

namespace Dropship.Controllers;

[ApiController]
[Route("stock")]
public class StockController : ControllerBase
{
    private readonly StockRepository _stockRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly KardexService _kardexService;
    private readonly ILogger<StockController> _logger;

    public StockController(StockRepository stockRepository, SupplierRepository supplierRepository, KardexService kardexService, ILogger<StockController> logger)
    {
        _stockRepository = stockRepository;
        _supplierRepository = supplierRepository;
        _kardexService = kardexService;
        _logger = logger;
    }

    [HttpPut("{supplierId}/{sku}")]
    public async Task<IActionResult> UpdateStock(string supplierId, string sku, [FromBody] UpdateStockRequest request)
    {
        _logger.LogInformation("[STOCK] Updating stock - Supplier: {SupplierId}, SKU: {Sku}, Quantity: {Quantity}", supplierId, sku, request.Quantity);
        
        try
        {
            var supplier = await _supplierRepository.GetSupplierAsync(supplierId);
            if (supplier == null)
            {
                _logger.LogWarning("[STOCK] Supplier not found: {SupplierId}", supplierId);
                return BadRequest($"Supplier {supplierId} does not exist");
            }

            var (skuExists, productId) = await _stockRepository.VerifySkuExistsAsync(sku);
            if (!skuExists)
            {
                _logger.LogWarning("[STOCK] SKU not found: {Sku}", sku);
                return BadRequest($"Product does not have sku {sku}");
            }

            await _stockRepository.UpdateProductStockAsync(supplierId, productId, sku, request.Quantity);
            
            var kardex = new KardexDomain
            {
                SK = sku,
                ProductId = productId,
                Quantity = request.Quantity,
                Operation = "set",
                SupplierId = supplierId
            };
            
            await _kardexService.AddToKardexAsync(kardex);

            _logger.LogInformation("[STOCK] Stock updated successfully - Supplier: {SupplierId}, SKU: {Sku}, ProductId: {ProductId}, NewQuantity: {Quantity}", supplierId, sku, productId, request.Quantity);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOCK] Error updating stock - Supplier: {SupplierId}, SKU: {Sku}", supplierId, sku);
            return BadRequest(ex.Message);
        }
    }
}