using Dropship.Domain;
using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciar fornecedores (suppliers)
/// </summary>
[ApiController]
[Route("suppliers")]
[Authorize]
public class SupplierController(SupplierRepository supplierRepository, 
                                UserRepository userRepository, 
                                ProductRepository productRepository,
                                ProductSkuSupplierRepository productSkuSupplierRepository,
                                ProductSupplierRepository productSupplierRepository,
                                SkuRepository skuRepository,
                                ILogger<SupplierController> logger)
    : ControllerBase
{
    /// <summary>
    /// Obtém informações de um fornecedor pelo ID
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <returns>Informações do fornecedor</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplier(string id)
    {
        logger.LogInformation("Fetching supplier with ID: {SupplierId}", id);

        try
        {
            var supplier = await supplierRepository.GetSupplierAsync(id);
            if (supplier == null)
            {
                logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            var response = supplier.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching supplier with ID: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lista todos os fornecedores (usando GSI_RELATIONS_LOOKUP)
    /// </summary>
    /// <returns>Lista de fornecedores ordenados por prioridade</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SupplierListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSuppliers()
    {
        logger.LogInformation("Fetching all suppliers from database");

        try
        {
            var suppliers = await supplierRepository.GetAllSuppliersAsync();
            var response = suppliers.ToListResponse();

            logger.LogInformation("Retrieved {Count} suppliers", response.Total);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching suppliers");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cria um novo fornecedor
    /// </summary>
    /// <param name="request">Dados do novo fornecedor</param>
    /// <returns>Fornecedor criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        logger.LogInformation("Creating supplier with name: {SupplierName}", request.Name);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid supplier request - ModelState errors");
                return BadRequest(ModelState);
            }
    
            var supplier = await supplierRepository.CreateSupplierAsync(request);
            var response = supplier.ToResponse();

            var userEmail = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email")?.Value;

            await userRepository.SetResourceId(userEmail, supplier.Id);
            logger.LogInformation("Supplier created successfully with ID: {SupplierId}", supplier.Id);
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating supplier");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Atualiza um fornecedor existente
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <param name="request">Dados a atualizar</param>
    /// <returns>Fornecedor atualizado</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSupplier(string id, [FromBody] UpdateSupplierRequest request)
    {
        logger.LogInformation("Updating supplier with ID: {SupplierId}", id);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid supplier request - ModelState errors");
                return BadRequest(ModelState);
            }

            var supplier = await supplierRepository.UpdateSupplierAsync(id, request);
            if (supplier == null)
            {
                logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            var response = supplier.ToResponse();
            logger.LogInformation("Supplier updated successfully with ID: {SupplierId}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating supplier with ID: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deleta um fornecedor
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <returns>Resultado da operação</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSupplier(string id)
    {
        logger.LogInformation("Deleting supplier with ID: {SupplierId}", id);

        try
        {
            var success = await supplierRepository.DeleteSupplierAsync(id);
            if (!success)
            {
                logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            logger.LogInformation("Supplier deleted successfully with ID: {SupplierId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting supplier with ID: {SupplierId}", id);
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
    [HttpPost("products/{productId}")]
    [ProducesResponseType(typeof(ProductSkuSupplierListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkSupplierToProduct(
        string productId,
        [FromBody] SupplierToProductRequest request)
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

            var productSkuSuppliers = new List<ProductSkuSupplierDomain>();
            foreach (var sku in skus)
            {
                var skuRequest = request.Skus.FirstOrDefault(x => x.Sku == sku.Sku);

                if (skuRequest == null)
                {
                    logger.LogWarning("Sku reference missing for SKU {sku}", sku.Sku);
                    return BadRequest(new { error = $"Sku reference missing for sku:{sku.Sku}" });
                }
                
                if(skuRequest.Price == 0)
                {
                    logger.LogWarning("Pricing missing for sku: {sku.Sku}", sku.Sku);
                    return BadRequest(new { error = $"Missing price for SKU: {sku.Sku}" });
                }
                
                if(string.IsNullOrWhiteSpace(skuRequest.SkuSupplier))
                {
                    logger.LogWarning("Sku supplier missing for sku: {sku.Sku}", sku.Sku);
                    return BadRequest(new { error = $"Missing SKU supplier for sku: {sku.Sku}" });
                }
                
                productSkuSuppliers.Add( ProductSkuSupplierBuilder.Create(productId, sku.Sku, skuRequest.SkuSupplier, supplierId, skuRequest.Price) );
            }
            
            var linkedRecords = await productSkuSupplierRepository.LinkSupplierToProductAsync(productSkuSuppliers);

            await productSupplierRepository.CreateProductSupplierAsync(
                productId,
                supplierId,
                product.Name,
                productSkuSuppliers.Count
            );

            logger.LogInformation(
                "Supplier linked to product successfully - ProductId: {ProductId}, SupplierId: {SupplierId}, SKUs: {SkuCount}",
                productId, supplierId, linkedRecords.Count);

            return Ok(
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
    /// Obtém todos os produtos fornecidos pelo fornecedor autenticado
    /// O ID do fornecedor é obtido automaticamente da claim "resourceId" do usuário autenticado
    /// </summary>
    /// <returns>Lista de produtos fornecidos com informações de preço e quantidade de SKUs</returns>
    [HttpGet("products")]
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
    /// Obtém todos os fornecedores disponíveis para um SKU específico
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <returns>Lista de fornecedores com seus preços de produção</returns>
    [HttpGet("products/{productId}/skus/{sku}")]
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
    [HttpGet("products/{productId}/skus")]
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
    [HttpPut("products/{productId}/skus/{sku}")]
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

            var existingLink = await productSkuSupplierRepository.GetProductSkuSupplierAsync(
                productId, sku, supplierId);
            
            if (existingLink == null)
            {
                logger.LogWarning("Product-SKU-Supplier link not found - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}", productId, sku, supplierId);
                return NotFound(new { error = "This supplier does not have a link with this SKU. Please create the link first." });
            }
            
            var updated = await productSkuSupplierRepository.UpdateSkuSupplier(
                productId, sku, supplierId, request.SkuSupplier, request.price, request.Quantity);

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
    /// Atualiza o preço, sku ou quantidade de múltiplos SKUs fornecidos por um fornecedor
    /// </summary>
    /// <param name="productId">ID do produto</param> <param name="sku">Código do SKU</param>
    /// <param name="request">Novos preço e quantidade</param>
    /// <returns>Registro atualizado</returns>
    [HttpPut("products/{productId}/skus")]
    [ProducesResponseType(typeof(ProductSkuSupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSupplierPricingList(
        string productId,
        [FromBody] SupplierUpdateRequest request)
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation(
            "Updating supplier pricing - ProductId: {ProductId}, SupplierId: {SupplierId}",productId, supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Invalid parameters provided");
                return BadRequest(new { error = "Product ID, SKU, and Supplier ID are required" });
            }

            foreach (var sku in request.Skus)
            {
                var existingLink = await productSkuSupplierRepository.GetProductSkuSupplierAsync(productId, sku.Sku, supplierId);
            
                if (existingLink == null)
                {
                    logger.LogWarning("Product-SKU-Supplier link not found - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}", productId, sku, supplierId);
                    return NotFound(new { error = "This supplier does not have a link with this SKU. Please create the link first." });
                }
            }

            foreach (var sku in request.Skus)
            {
                try
                {
                    await productSkuSupplierRepository.UpdateSkuSupplier(productId, sku.Sku, supplierId, sku.SkuSupplier, sku.Price, sku.Quantity);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating SKU supplier - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}", productId, sku.Sku, supplierId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
                }
            }

            return Ok(new { message = "Supplier pricing updated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating supplier pricing - ProductId: {ProductId}",productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Remove um fornecedor de um produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="sku">Código do SKU</param>
    /// <param name="supplierId">ID do fornecedor</param>
    /// <returns>Status de sucesso</returns>
    [HttpDelete("products/{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSupplierFromSku(string productId)
    {
        var supplierId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation(
            "Removing supplier from SKU - ProductId: {ProductId}, SupplierId: {SupplierId}",
            productId, supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Invalid parameters provided");
                return BadRequest(new { error = "Product ID, SKU, and Supplier ID are required" });
            }

            var suppliersSku = await productSkuSupplierRepository.GetSkusBySupplier(productId, supplierId);
            foreach (var supplierSku in suppliersSku)
            {
                await productSkuSupplierRepository.RemoveSupplierFromSku(productId, supplierSku.Sku, supplierId);
            }

            await productSupplierRepository.RemoveProductSupplierAsync(supplierId, productId);
            logger.LogInformation("Supplier removed successfully - ProductId: {ProductId}", productId);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing supplier from SKU - ProductId: {ProductId}", productId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
