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
    ProductSkuSupplierRepository productSkuSupplierRepository,
    ProductSupplierRepository productSupplierRepository,
    ProductSkuSellerRepository productSkuSellerRepository,
    ProductSellerRepository productSellerRepository,
    SellerRepository sellerRepository,
    SupplierRepository supplierRepository,
    ILogger<ProductController> logger)
    : ControllerBase
{
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

    #region Supplier Integration

    /// <summary>
    /// Obtém todos os produtos fornecidos pelo fornecedor autenticado
    /// O ID do fornecedor é obtido automaticamente da claim "resourceId" do usuário autenticado
    /// </summary>
    /// <returns>Lista de produtos fornecidos com informações de preço e quantidade de SKUs</returns>
    [HttpGet("supplier")]
    [ProducesResponseType(typeof(ProductSupplierListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductsBySupplier()
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Getting products for supplier - SupplierId: {SupplierId}", supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Supplier ID not found in claims");
                return BadRequest(new { error = "Supplier ID not found in authentication claims" });
            }

            var products = await productSupplierRepository.GetProductsBySupplierAsync(supplierId);
            if (products == null || products.Count == 0)
            {
                logger.LogWarning("No products found for supplier - SupplierId: {SupplierId}", supplierId);
                return NotFound(new { error = "No products found for this supplier" });
            }

            logger.LogInformation("Found {Count} products for supplier - SupplierId: {SupplierId}",
                products.Count, supplierId);

            return Ok(products.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products for supplier - SupplierId: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Vincula um fornecedor a um produto com preço de produção
    /// Mapeia automaticamente todos os SKUs do produto e cria registros na estrutura:
    /// PK: Product#{productId}
    /// SK: Sku#{sku}#Supplier#{supplierId}
    /// O ID do fornecedor é obtido da claim "resourceId" do usuário autenticado
    /// </summary>
    /// <param name="productId">ID do produto que deve existir no banco de dados</param>
    /// <param name="request">Dados do fornecimento (preço de produção, prioridade)</param>
    /// <returns>Lista de registros criados (um por SKU do produto)</returns>
    [HttpPost("{productId}/suppliers")]
    [ProducesResponseType(typeof(ProductSkuSupplierListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkSupplierToProduct(
        string productId,
        [FromBody] LinkSupplierToProductRequest request)
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Linking supplier to product - ProductId: {ProductId}, SupplierId: {SupplierId}",
            productId, supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Invalid product or supplier ID provided");
                return BadRequest(new { error = "Product ID and Supplier ID are required" });
            }

            var product = await productRepository.GetProductByIdAsync(productId);
            if (product == null)
            {
                logger.LogWarning("Product not found - ProductId: {ProductId}", productId);
                return NotFound(new { error = "Product not found" });
            }

            var supplier = await supplierRepository.GetSupplierAsync(supplierId);
            if (supplier == null)
            {
                logger.LogWarning("Supplier not found - SupplierId: {SupplierId}", supplierId);
                return NotFound(new { error = "Supplier not found" });
            }

            var skus = await skuRepository.GetSkusByProductIdAsync(productId);
            if (skus == null || skus.Count == 0)
            {
                logger.LogWarning("No SKUs found for product - ProductId: {ProductId}", productId);
                return BadRequest(new { error = "Product has no SKUs" });
            }

            var skuCodes = skus.Select(s => s.Sku).ToList();

            // Vincular fornecedor ao produto (cria registros para cada SKU)
            var linkedRecords = await productSkuSupplierRepository.LinkSupplierToProductAsync(
                productId,
                supplierId,
                request.ProductionPrice,
                skuCodes
            );

            await productSupplierRepository.CreateProductSupplierAsync(
                productId,
                supplierId,
                product.Name,
                request.ProductionPrice,
                skuCodes.Count
            );

            logger.LogInformation(
                "Supplier linked to product successfully - ProductId: {ProductId}, SupplierId: {SupplierId}, SKUs: {SkuCount}",
                productId, supplierId, linkedRecords.Count);

            return CreatedAtAction(
                nameof(GetSuppliersBySku),
                new { productId, sku = skuCodes[0] },
                linkedRecords.ToListResponse()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error linking supplier to product - ProductId: {ProductId}, SupplierId: {SupplierId}",
                productId, supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém todos os fornecedores disponíveis para um SKU específico
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <returns>Lista de fornecedores com seus preços de produção</returns>
    [HttpGet("{productId}/skus/{sku}/suppliers")]
    [ProducesResponseType(typeof(ProductSkuSupplierListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSuppliersBySku(string productId, string sku)
    {
        logger.LogInformation("Getting suppliers for SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku))
            {
                logger.LogWarning("Invalid product ID or SKU provided");
                return BadRequest(new { error = "Product ID and SKU are required" });
            }

            var suppliers = await productSkuSupplierRepository.GetSuppliersBySku(productId, sku);
            if (suppliers == null || suppliers.Count == 0)
            {
                logger.LogWarning("No suppliers found for SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return NotFound(new { error = "No suppliers found for this SKU" });
            }

            return Ok(suppliers.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting suppliers for SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém todos os SKUs fornecidos pelo fornecedor autenticado para um produto específico
    /// O ID do fornecedor é obtido automaticamente da claim "resourceId" do usuário autenticado
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de SKUs fornecidos com seus preços de produção</returns>
    [HttpGet("{productId}/suppliers/skus")]
    [ProducesResponseType(typeof(ProductSkuSupplierListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkusBySupplier(string productId)
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Getting SKUs for supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
            productId, supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                logger.LogWarning("Invalid product ID provided");
                return BadRequest(new { error = "Product ID is required" });
            }

            if (string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Supplier ID not found in claims");
                return BadRequest(new { error = "Supplier ID not found in authentication claims" });
            }

            var skus = await productSkuSupplierRepository.GetSkusBySupplier(productId, supplierId);
            if (skus == null || skus.Count == 0)
            {
                logger.LogWarning("No SKUs found for supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
                    productId, supplierId);
                return NotFound(new { error = "No SKUs found for this supplier in this product" });
            }

            logger.LogInformation("Found {Count} SKUs for supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
                skus.Count, productId, supplierId);

            return Ok(skus.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting SKUs for supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
                productId, supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Atualiza o preço de produção e quantidade de um SKU fornecido por um fornecedor
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <param name="supplierId">ID do fornecedor</param>
    /// <param name="request">Novos preço e quantidade</param>
    /// <returns>Registro atualizado</returns>
    [HttpPut("{productId}/skus/{sku}/suppliers")]
    [ProducesResponseType(typeof(ProductSkuSupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSupplierPricing(
        string productId,
        string sku,
        [FromBody] UpdateSupplierPricingRequest request)
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation(
            "Updating supplier pricing - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
            productId, sku, supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku) ||
                string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Invalid parameters provided");
                return BadRequest(new { error = "Product ID, SKU, and Supplier ID are required" });
            }

            var updated = await productSkuSupplierRepository.UpdateSupplierPricingAsync(
                productId, sku, supplierId, request.ProductionPrice, request.Quantity);

            if (updated == null)
            {
                logger.LogWarning("Product-SKU-Supplier record not found - ProductId: {ProductId}, SKU: {Sku}",
                    productId, sku);
                return NotFound(new { error = "Product-SKU-Supplier record not found" });
            }

            return Ok(updated.ToResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating supplier pricing - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove um fornecedor de um SKU específico
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <param name="supplierId">ID do fornecedor</param>
    /// <returns>Status de sucesso</returns>
    [HttpDelete("{productId}/skus/{sku}/suppliers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSupplierFromSku(string productId, string sku)
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation(
            "Removing supplier from SKU - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
            productId, sku, supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku) ||
                string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Invalid parameters provided");
                return BadRequest(new { error = "Product ID, SKU, and Supplier ID are required" });
            }

            var removed = await productSkuSupplierRepository.RemoveSupplierFromSku(productId, sku, supplierId);
            if (!removed)
            {
                logger.LogWarning("Failed to remove supplier - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return NotFound(new { error = "Product-SKU-Supplier record not found" });
            }

            logger.LogInformation("Supplier removed successfully - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing supplier from SKU - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Seller Integration

    /// <summary>
    /// Obtém todos os produtos vinculados a um vendedor em um marketplace
    /// O ID do vendedor é obtido automaticamente da claim "resourceId" do usuário autenticado
    /// </summary>
    /// <returns>Lista de produtos vinculados com informações de SKUs</returns>
    [HttpGet("sellers")]
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
    [HttpPost("{productId}/sellers")]
    [ProducesResponseType(typeof(ProductSkuSellerListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkSellerToProduct(
        string productId,
        [FromBody] UpdateSellerPriceRequest request)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation(
            "Linking seller to product - ProductId: {ProductId}, SellerId: {SellerId}",
            productId, sellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Invalid product or seller ID provided");
                return BadRequest(new { error = "Product ID and Seller ID are required" });
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

            var store = await sellerRepository.GetSellerByIdAsync(sellerId);
            
            // Vincular vendedor ao produto (cria registros para cada SKU)
            var linkedRecords = await productSkuSellerRepository.LinkSellerToProductAsync(
                productId,
                sellerId,
                "shopee",
                store.ShopId,
                request.Price,
                skus
            );

            // Criar registro META para busca rápida de produtos por vendedor
            await productSellerRepository.CreateProductSellerAsync(
                productId,
                product.Name,
                sellerId,
                "shopee",
                store.ShopId,
                linkedRecords.Count
            );

            logger.LogInformation(
                "Seller linked to product successfully - ProductId: {ProductId}, SellerId: {SellerId}, SKUs: {SkuCount}",
                productId, sellerId, linkedRecords.Count);

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
    /// Obtém todos os SKUs de um produto vinculados ao vendedor autenticado
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <returns>Lista de SKUs vinculados</returns>
    [HttpGet("{productId}/sellers/skus")]
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
                return BadRequest(new { error = "Product ID and marketplace are required" });
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
    [HttpPut("{productId}/sellers/skus/{sku}/price")]
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
                return BadRequest(new { error = "Product ID, SKU, marketplace, and Seller ID are required" });
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
    /// Remove um vendedor de um SKU
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <returns>Status de sucesso</returns>
    [HttpDelete("{productId}/sellers/skus/{sku}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSellerFromSku(
        string productId,
        string sku)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Removing seller from SKU - ProductId: {ProductId}, SKU: {Sku}, SellerId: {SellerId}",
            productId, sku, sellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Missing required parameters");
                return BadRequest(new { error = "Product ID, SKU, marketplace, and Seller ID are required" });
            }

            var removed =
                await productSkuSellerRepository.RemoveSellerFromSkuAsync(productId, sku, sellerId, "shopee");
            if (!removed)
            {
                logger.LogWarning("Failed to remove seller - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return NotFound(new { error = "Product-SKU-Seller record not found" });
            }

            logger.LogInformation("Seller removed from SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing seller from SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
    #endregion
}
