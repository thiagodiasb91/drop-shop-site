using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Domain;
using Dropship.Services;
using Dropship.Requests;
using Microsoft.AspNetCore.Authorization;

namespace Dropship.Controllers;

[ApiController]
[Route("stock")]
[Authorize]
public class StockController(
    StockRepository stockRepository,
    SupplierRepository supplierRepository,
    KardexService kardexService,
    ILogger<StockController> logger)
    : ControllerBase
{
    [HttpPut("{sku}")]
    public async Task<IActionResult> UpdateStock(string sku, [FromBody] UpdateStockRequest request)
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("[STOCK] Updating stock - Supplier: {SupplierId}, SKU: {Sku}, Quantity: {Quantity}", supplierId, sku, request.Quantity);
        
        try
        {
            var supplier = await supplierRepository.GetSupplierAsync(supplierId);
            if (supplier == null)
            {
                logger.LogWarning("[STOCK] Supplier not found: {SupplierId}", supplierId);
                return BadRequest($"Supplier {supplierId} does not exist");
            }

            var (skuExists, productId) = await stockRepository.VerifySkuExistsAsync(sku);
            if (!skuExists)
            {
                logger.LogWarning("[STOCK] SKU not found: {Sku}", sku);
                return BadRequest($"Product does not have sku {sku}");
            }

            await stockRepository.UpdateProductStockAsync(supplierId, productId, sku, request.Quantity);
            
            var kardex = new KardexDomain
            {
                SK = sku,
                ProductId = productId,
                Quantity = request.Quantity,
                Operation = "set",
                SupplierId = supplierId
            };
            
            await kardexService.AddToKardexAsync(kardex);

            logger.LogInformation("[STOCK] Stock updated successfully - Supplier: {SupplierId}, SKU: {Sku}, ProductId: {ProductId}, NewQuantity: {Quantity}", supplierId, sku, productId, request.Quantity);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[STOCK] Error updating stock - Supplier: {SupplierId}, SKU: {Sku}", supplierId, sku);
            return BadRequest(ex.Message);
        }
    }
}