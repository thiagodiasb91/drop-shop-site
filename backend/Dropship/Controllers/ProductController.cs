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
public class ProductController : ControllerBase
{
    private readonly ProductRepository _productRepository;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductRepository productRepository, ILogger<ProductController> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

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
        _logger.LogInformation("Getting product - ProductId: {ProductId}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product not found - ProductId: {ProductId}", id);
                return NotFound(new { error = "Product not found" });
            }

            var response = product.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product - ProductId: {ProductId}", id);
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
        _logger.LogInformation("Fetching all products");

        try
        {
            var products = await _productRepository.GetAllProductsAsync();
            var response = products.ToListResponse();

            _logger.LogInformation("Retrieved {Count} products", response.Total);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cria um novo produto
    /// </summary>
    /// <param name="request">Dados do novo produto</param>
    /// <returns>Produto criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("Creating product - ProductName: {ProductName}", request.ProductName);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product request - ModelState errors");
                return BadRequest(ModelState);
            }

            var product = await _productRepository.CreateProductAsync(request);
            var response = product.ToResponse();

            _logger.LogInformation("Product created successfully - ProductId: {ProductId}", product.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Atualiza um produto existente
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <param name="request">Dados a atualizar</param>
    /// <returns>Produto atualizado</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateProductRequest request)
    {
        _logger.LogInformation("Updating product - ProductId: {ProductId}", id);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product request - ModelState errors");
                return BadRequest(ModelState);
            }

            var product = await _productRepository.UpdateProductAsync(id, request);
            if (product == null)
            {
                _logger.LogWarning("Product not found for update - ProductId: {ProductId}", id);
                return NotFound(new { error = "Product not found" });
            }

            var response = product.ToResponse();
            _logger.LogInformation("Product updated successfully - ProductId: {ProductId}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product - ProductId: {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deleta um produto
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <returns>Status da exclusão</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        _logger.LogInformation("Deleting product - ProductId: {ProductId}", id);

        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            var success = await _productRepository.DeleteProductAsync(id);
            if (!success)
            {
                _logger.LogWarning("Product not found for deletion - ProductId: {ProductId}", id);
                return NotFound(new { error = "Product not found" });
            }

            _logger.LogInformation("Product deleted successfully - ProductId: {ProductId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product - ProductId: {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
