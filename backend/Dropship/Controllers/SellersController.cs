using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;
using Dropship.Domain;
using Dropship.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dropship.Controllers;

[Route("sellers")]
public class SellersController(ILogger<SellersController> logger,
                            SellerRepository sellerRepository,
                            UserRepository userRepository,
                            ShopeeService shopeeService,
                            ProductRepository productRepository,
                            SkuRepository skuRepository,
                            ProductSkuSellerRepository productSkuSellerRepository,
                            ProductSellerRepository productSellerRepository,
                            ProductSupplierRepository productSupplierRepository

     ) : ControllerBase 
{
    /// <summary>
    /// Obtém todos os produtos vinculados a um vendedor em um marketplace
    /// O ID do vendedor é obtido automaticamente da claim "resourceId" do usuário autenticado
    /// </summary>
    /// <returns>Lista de produtos vinculados com informações de SKUs</returns>
    [HttpGet("products")]
    [ProducesResponseType(typeof(ProductSellerListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductsBySeller()
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Getting products for seller - SellerId: {SellerId}",sellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Seller ID not found in claims");
                return BadRequest(new { error = "Seller ID not found in authentication claims" });
            }

            var products = await productSellerRepository.GetProductsBySellerAsync(sellerId);
            if (products == null || products.Count == 0)
            {
                logger.LogWarning("No products found for seller - SellerId: {SellerId}", sellerId);
                return NotFound(new { error = "No products found for this seller in this marketplace" });
            }

            logger.LogInformation("Found {Count} products for seller - SellerId: {SellerId}",
                products.Count, sellerId);

            return Ok(products.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products for seller - SellerId: {SellerId}", sellerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Vincula um vendedor a um produto com preço
    /// Cria um registro META e registros para cada SKU
    /// A quantidade é atualizada automaticamente via sistema
    /// </summary>
    /// <param name="productId">ID do produto que deve existir no banco de dados</param>
    /// <param name="request">Dados do vínculo (preço, marketplace, store_id, SKU mappings)</param>
    /// <returns>Lista de SKUs vinculados</returns>
    [HttpPost("products/{productId}/suppliers/{supplierId}")]
    [ProducesResponseType(typeof(ProductSkuSellerListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkSellerToProduct(
        string productId,
        string supplierId,
        [FromBody] LinkSellerToProductRequest request)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation(
            "Linking seller to product - ProductId: {ProductId}, SellerId: {SellerId}",
            productId, sellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Invalid product, supplierId or seller ID provided");
                return BadRequest(new { error = "Product ID, supplierId and Seller ID are required" });
            }

            // Validar que o produto existe
            var product = await productRepository.GetProductByIdAsync(productId);
            if (product == null)
            {
                logger.LogWarning("Product not found - ProductId: {ProductId}", productId);
                return NotFound(new { error = "Product not found" });
            }

            // Validar que o produto tem SKUs
            var skus = await skuRepository.GetSkusByProductIdAsync(productId);
            if (skus == null || skus.Count == 0)
            {
                logger.LogWarning("No SKUs found for product - ProductId: {ProductId}", productId);
                return BadRequest(new { error = "Product has no SKUs" });
            }

            var seller = await sellerRepository.GetSellerByIdAsync(sellerId);
            var skusToPublish = new List<ProductSkuSellerDomain>();
            foreach (var mapping in skus)
            {
                // Validar que cada SKU existe no produto
                var skuMapping = request.SkuMappings.FirstOrDefault(s => s.Sku == mapping.Sku);
                
                var domain = ProductSkuSellerFactory.Create(
                    productId,
                    mapping.Sku,
                    sellerId,
                    "shopee",
                    seller.ShopId,
                    skuMapping?.Price ?? request.Price,
                    mapping.Color,
                    mapping.Size,
                    supplierId
                );
                skusToPublish.Add(domain);
            }

            // Vincular vendedor ao produto (cria registros para cada domain)
            var linkedRecords = await productSkuSellerRepository.LinkSellerToProductAsync(skusToPublish);

            // Criar registro META para busca rápida de produtos por vendedor
            var productSeller = await productSellerRepository.CreateProductSellerAsync(
                productId,
                product.Name,
                sellerId,
                "shopee",
                seller.ShopId,
                request.Price,
                supplierId,
                linkedRecords.Count
            );

            logger.LogInformation(
                "Seller linked to product successfully - ProductId: {ProductId}, SellerId: {SellerId}, SKUs: {SkuCount}",
                productId, sellerId, linkedRecords.Count);

            await shopeeService.UploadProduct(productSeller, skusToPublish);
            
            return CreatedAtAction(
                nameof(GetSkusBySellerInProduct),
                new { productId },
                linkedRecords.ToListResponse()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error linking seller to product - ProductId: {ProductId}, SellerId: {SellerId}",
                productId, sellerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Atualiza o preço de um produto para todos os seus SKUs para o vendedor autenticado
    /// Similar ao Supplier, atualiza o preço em todos os SKUs vinculados ao produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="request">Novo preço</param>
    /// <returns>Lista de SKUs com preço atualizado</returns>
    [HttpPut("products/{productId}/suppliers/{supplierId}")]
    [ProducesResponseType(typeof(ProductSkuSellerListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSellerProduct(
        string productId,
        [FromBody] UpdateSellerPriceRequest request)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Updating seller product price - ProductId: {ProductId}, SellerId: {SellerId}, Price: {Price}",
            productId, sellerId, request.Price);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Missing required parameters");
                return BadRequest(new { error = "Product ID and Seller ID are required" });
            }

            // Obter todos os SKUs do vendedor neste produto
            var skus = await productSkuSellerRepository.GetSkusBySellerAsync(productId, sellerId);
            if (skus == null || skus.Count == 0)
            {
                logger.LogWarning("No SKUs found for seller product - ProductId: {ProductId}", productId);
                return NotFound(new { error = "No SKUs found for this seller in this product" });
            }

            // Atualizar preço de todos os SKUs
            var updatedSkus = new List<ProductSkuSellerDomain>();
            foreach (var sku in skus)
            {
                var updated = await productSkuSellerRepository.UpdatePriceAsync(
                    productId, sku.Sku, sellerId, "shopee", request.Price);
                if (updated != null)
                {
                    updatedSkus.Add(updated);
                }
            }

            if (updatedSkus.Count == 0)
            {
                logger.LogWarning("Failed to update any SKUs - ProductId: {ProductId}", productId);
                return NotFound(new { error = "Failed to update product SKUs" });
            }

            logger.LogInformation("Seller product price updated - ProductId: {ProductId}, SellerId: {SellerId}, UpdatedSKUs: {Count}",
                productId, sellerId, updatedSkus.Count);

            return Ok(updatedSkus.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating seller product price - ProductId: {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém todos os SKUs de um produto vinculados ao vendedor autenticado
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de SKUs vinculados</returns>
    [HttpGet("products/{productId}/skus")]
    [ProducesResponseType(typeof(ProductSkuSellerListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkusBySellerInProduct(string productId)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Getting SKUs for seller in product - ProductId: {ProductId}, SellerId: {SellerId}",
            productId, sellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                logger.LogWarning("Missing required parameters");
                return BadRequest(new { error = "Product ID is required" });
            }

            if (string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Seller ID not found in claims");
                return BadRequest(new { error = "Seller ID not found in authentication claims" });
            }

            var skus = await productSkuSellerRepository.GetSkusBySellerAsync(productId, sellerId);
            if (skus == null || skus.Count == 0)
            {
                logger.LogWarning("No SKUs found - ProductId: {ProductId}, SellerId: {SellerId}", productId, sellerId);
                return NotFound(new { error = "No SKUs found for this seller in this product" });
            }

            return Ok(skus.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting SKUs for seller - ProductId: {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
    

    /// <summary>
    /// Atualiza o preço de um SKU para o vendedor autenticado
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <param name="request">Novo preço</param>
    /// <returns>Registro atualizado</returns>
    [HttpPut("products/{productId}/skus/{sku}/price")]
    [ProducesResponseType(typeof(ProductSkuSellerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSellerPrice(
        string productId,
        string sku,
        [FromBody] UpdateSellerPriceRequest request)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Updating seller price - ProductId: {ProductId}, SKU: {Sku}, Price: {Price}",
            productId, sku, request.Price);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Missing required parameters");
                return BadRequest(new { error = "Product ID, SKU, and Seller ID are required" });
            }

            var updated = await productSkuSellerRepository.UpdatePriceAsync(
                productId, sku, sellerId, "shopee", request.Price);

            if (updated == null)
            {
                logger.LogWarning("Record not found - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return NotFound(new { error = "Product-SKU-Seller record not found" });
            }

            return Ok(updated.ToResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating price - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove um vendedor de um produto (remove todos os SKUs vinculados)
    /// O vendedor é obtido automaticamente da claim "resourceId" do usuário autenticado
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Status de sucesso</returns>
    [HttpDelete("products/{productId}/suppliers/{supplierId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSellerFromProduct(string productId, string supplierId)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation(
            "Removing seller from product - ProductId: {ProductId}, SellerId: {SellerId}",
            productId, sellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Invalid parameters provided");
                return BadRequest(new { error = "Product ID and Seller ID are required" });
            }

            // Obter todos os SKUs do vendedor neste produto
            var sellerSkus = await productSkuSellerRepository.GetSkusBySellerAsync(productId, sellerId, "shopee");
            
            // Remover cada SKU do vendedor
            foreach (var sellerSku in sellerSkus)
            {
                await productSkuSellerRepository.RemoveSellerFromSkuAsync(
                    productId, sellerSku.Sku, sellerId, "shopee", supplierId);
            }

            // Remover registro META do vendedor no produto
            await productSellerRepository.RemoveProductSellerAsync(sellerId, "shopee", productId);

            logger.LogInformation("Seller removed successfully from product - ProductId: {ProductId}, SellerId: {SellerId}, RemovedSKUs: {Count}",
                productId, sellerId, sellerSkus.Count);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing seller from product - ProductId: {ProductId}, SellerId: {SellerId}",
                productId, sellerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Lista todos os SKUs de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de SKUs do produto</returns>
    [HttpGet("products/available")]
    [ProducesResponseType(typeof(SkuListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProductsAvailable()
    {
        logger.LogInformation("Getting all products");

        try
        {
            var skus = await productSupplierRepository.GetAllProductsWithSupplier();
            
            return Ok(skus.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting SKUs for product");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}