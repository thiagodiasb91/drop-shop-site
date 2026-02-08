using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciar SKUs (Stock Keeping Units)
/// Responsável por operações CRUD de variações de produtos
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

    /// <summary>
    /// Obtém um SKU específico de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <returns>Informações do SKU</returns>
    [HttpGet("{sku}")]
    [ProducesResponseType(typeof(SkuResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSku(string productId, string sku)
    {
        _logger.LogInformation("Getting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("Invalid product ID or SKU provided");
                return BadRequest(new { error = "Product ID and SKU are required" });
            }

            var skuDomain = await _skuRepository.GetSkuAsync(productId, sku);
            if (skuDomain == null)
            {
                _logger.LogWarning("SKU not found - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return NotFound(new { error = "SKU not found" });
            }

            return Ok(skuDomain.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lista todos os SKUs de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de SKUs do produto</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SkuListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkusByProduct(string productId)
    {
        _logger.LogInformation("Getting all SKUs for product - ProductId: {ProductId}", productId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                _logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            var skus = await _skuRepository.GetSkusByProductIdAsync(productId);
            return Ok(skus.ToListResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SKUs for product - ProductId: {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cria um novo SKU para um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="request">Dados do novo SKU</param>
    /// <returns>Informações do SKU criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SkuResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSku(string productId, [FromBody] CreateSkuRequest request)
    {
        _logger.LogInformation("Creating SKU - ProductId: {ProductId}, SKU: {Sku}", productId, request.Sku);

        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                _logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Sku))
            {
                _logger.LogWarning("Invalid SKU request");
                return BadRequest(new { error = "SKU code is required" });
            }

            // Validar que o productId do request corresponde ao da URL
            if (!string.IsNullOrEmpty(request.ProductId) && request.ProductId != productId)
            {
                _logger.LogWarning("Product ID mismatch - URL: {UrlProductId}, Request: {RequestProductId}", 
                    productId, request.ProductId);
                return BadRequest(new { error = "Product ID in URL does not match request body" });
            }

            // Usar o productId da URL
            request.ProductId = productId;

            var createdSku = await _skuRepository.CreateSkuAsync(request);
            _logger.LogInformation("SKU created successfully - ProductId: {ProductId}, SKU: {Sku}", 
                productId, request.Sku);

            return CreatedAtAction(nameof(GetSku), new { productId, sku = request.Sku }, createdSku.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SKU - ProductId: {ProductId}, SKU: {Sku}", 
                productId, request.Sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Atualiza um SKU existente
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <param name="request">Dados a serem atualizados</param>
    /// <returns>Informações do SKU atualizado</returns>
    [HttpPut("{sku}")]
    [ProducesResponseType(typeof(SkuResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSku(string productId, string sku, [FromBody] UpdateSkuRequest request)
    {
        _logger.LogInformation("Updating SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("Invalid product ID or SKU provided");
                return BadRequest(new { error = "Product ID and SKU are required" });
            }


            var updatedSku = await _skuRepository.UpdateSkuAsync(productId, sku, request);
            if (updatedSku == null)
            {
                _logger.LogWarning("SKU not found for update - ProductId: {ProductId}, SKU: {Sku}", 
                    productId, sku);
                return NotFound(new { error = "SKU not found" });
            }

            _logger.LogInformation("SKU updated successfully - ProductId: {ProductId}, SKU: {Sku}", 
                productId, sku);
            return Ok(updatedSku.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deleta um SKU
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <returns>Resultado da operação</returns>
    [HttpDelete("{sku}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSku(string productId, string sku)
    {
        _logger.LogInformation("Deleting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("Invalid product ID or SKU provided");
                return BadRequest(new { error = "Product ID and SKU are required" });
            }

            var success = await _skuRepository.DeleteSkuAsync(productId, sku);
            if (!success)
            {
                _logger.LogWarning("SKU not found for deletion - ProductId: {ProductId}, SKU: {Sku}", 
                    productId, sku);
                return NotFound(new { error = "SKU not found" });
            }

            _logger.LogInformation("SKU deleted successfully - ProductId: {ProductId}, SKU: {Sku}", 
                productId, sku);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Atualiza apenas a quantidade de um SKU
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <param name="quantity">Nova quantidade</param>
    /// <returns>Informações do SKU atualizado</returns>
    [HttpPatch("{sku}/quantity")]
    [ProducesResponseType(typeof(SkuResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuantity(string productId, string sku, [FromQuery] int quantity)
    {
        _logger.LogInformation("Updating SKU quantity - ProductId: {ProductId}, SKU: {Sku}, Quantity: {Quantity}",
            productId, sku, quantity);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku))
            {
                _logger.LogWarning("Invalid product ID or SKU provided");
                return BadRequest(new { error = "Product ID and SKU are required" });
            }

            if (quantity < 0)
            {
                _logger.LogWarning("Invalid quantity provided");
                return BadRequest(new { error = "Quantity cannot be negative" });
            }

            var updatedSku = await _skuRepository.UpdateSkuQuantityAsync(productId, sku, quantity);
            if (updatedSku == null)
            {
                _logger.LogWarning("SKU not found - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return NotFound(new { error = "SKU not found" });
            }

            _logger.LogInformation("SKU quantity updated successfully - ProductId: {ProductId}, SKU: {Sku}, Quantity: {Quantity}",
                productId, sku, quantity);
            return Ok(updatedSku.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SKU quantity - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
