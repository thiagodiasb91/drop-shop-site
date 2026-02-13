using Dropship.Domain;
using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciar produtos
/// </summary>
[ApiController]
[Route("products")]
public class ProductController(
    ProductRepository productRepository,
    SkuRepository skuRepository,
    SupplierRepository supplierRepository,
    ProductSupplierRepository productSupplierRepository,
    ILogger<ProductController> logger)
    : ControllerBase
{
    // ...existing code...
    /// <summary>
    /// Obtém um produto pelo ID
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <returns>Informações do produto</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(string id)
    {
        logger.LogInformation("Getting product - ProductId: {ProductId}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            var product = await productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                logger.LogWarning("Product not found - ProductId: {ProductId}", id);
                return NotFound(new { error = "Product not found" });
            }

            var response = product.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting product - ProductId: {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lista todos os produtos
    /// </summary>
    /// <returns>Lista de produtos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts()
    {
        logger.LogInformation("Fetching all products");

        try
        {
            var products = await productRepository.GetAllProductsAsync();
            var response = products.ToListResponse();

            logger.LogInformation("Retrieved {Count} products", response.Total);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching products");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém um SKU específico de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <returns>Informações do SKU</returns>
    [HttpGet("{productId}/skus/{sku}")]
    [ProducesResponseType(typeof(SkuResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSku(string productId, string sku)
    {
        logger.LogInformation("Getting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku))
            {
                logger.LogWarning("Invalid product ID or SKU provided");
                return BadRequest(new { error = "Product ID and SKU are required" });
            }

            var skuDomain = await skuRepository.GetSkuAsync(productId, sku);
            if (skuDomain == null)
            {
                logger.LogWarning("SKU not found - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return NotFound(new { error = "SKU not found" });
            }

            return Ok(skuDomain.ToResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Lista todos os SKUs de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de SKUs do produto</returns>
    [HttpGet("{productId}/skus")]
    [ProducesResponseType(typeof(SkuListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkusByProduct(string productId)
    {
        logger.LogInformation("Getting all SKUs for product - ProductId: {ProductId}", productId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            var skus = await skuRepository.GetSkusByProductIdAsync(productId);
            return Ok(skus.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting SKUs for product - ProductId: {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém todas as imagens de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de imagens do produto</returns>
    [HttpGet("{productId}/images")]
    [ProducesResponseType(typeof(ProductImagesListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductImages(string productId)
    {
        logger.LogInformation("Getting images for product - ProductId: {ProductId}", productId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            var images = await productRepository.GetImagesByProductIdAsync(productId);
            if (images == null || images.Count == 0)
            {
                logger.LogDebug("No images found for product - ProductId: {ProductId}", productId);
                return NotFound(new { error = "No images found for this product" });
            }

            logger.LogInformation("Retrieved {Count} images for product - ProductId: {ProductId}", images.Count, productId);
            return Ok(images.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting images for product - ProductId: {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém todos os fornecedores de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de fornecedores vinculados ao produto com nome</returns>
    [HttpGet("{productId}/suppliers")]
    [ProducesResponseType(typeof(ProductSupplierListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductSuppliers(string productId)
    {
        logger.LogInformation("Getting suppliers for product - ProductId: {ProductId}", productId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            var suppliers = await productSupplierRepository.GetSuppliersByProductIdAsync(productId);
            if (suppliers == null || suppliers.Count == 0)
            {
                logger.LogDebug("No suppliers found for product - ProductId: {ProductId}", productId);
                return NotFound(new { error = "No suppliers found for this product" });
            }

            // Enriquecer com dados do fornecedor (nome)
            var enrichedSuppliers = new List<dynamic>();
            foreach (var supplier in suppliers)
            {
                var supplierDetails = await supplierRepository.GetSupplierAsync(supplier);
                
                enrichedSuppliers.Add(new
                {
                    supplierId = supplierDetails.Id,
                    supplierName = supplierDetails?.Name ?? "Unknown",
                    
                });
            }

            logger.LogInformation("Retrieved {Count} suppliers for product - ProductId: {ProductId}", 
                enrichedSuppliers.Count, productId);
            
            return Ok(new
            {
                total = enrichedSuppliers.Count,
                items = enrichedSuppliers
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting suppliers for product - ProductId: {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
