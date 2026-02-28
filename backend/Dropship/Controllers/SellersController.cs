using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;
using Dropship.Domain;
using Dropship.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Dropship.Controllers;

[Route("sellers")]
public class SellersController(ILogger<SellersController> logger,
                            SellerRepository sellerRepository,
                            ShopeeApiService shopeeApiService,
                            ShopeeService shopeeService,
                            ProductRepository productRepository,
                            SkuRepository skuRepository,
                            ProductSkuSellerRepository productSkuSellerRepository,
                            ProductSellerRepository productSellerRepository,
                            ProductSupplierRepository productSupplierRepository,
                            InfinityPayLinkRepository linkRepository,
                            PaymentService paymentService,
                            OrderRepository orderRepository

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

            var products = await productSellerRepository.GetProductsBySeller(sellerId);
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
    /// Cria um registro META e registros para cada SKU com o fornecedor especificado
    /// A quantidade é atualizada automaticamente via sistema
    /// </summary>
    /// <param name="productId">ID do produto que deve existir no banco de dados</param>
    /// <param name="supplierId">ID do fornecedor</param>
    /// <param name="request">Dados do vínculo (preço, SKU mappings)</param>
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
            "Linking seller to product - ProductId: {ProductId}, SellerId: {SellerId}, SupplierId: {SupplierId}",
            productId, sellerId, supplierId);

        try
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(supplierId))
            {
                logger.LogWarning("Invalid product, supplierId or seller ID provided");
                return BadRequest(new { error = "Product ID, supplierId and Seller ID are required" });
            }
            
            var productSupplier = await productSupplierRepository.GetProductBySupplier(supplierId, productId);
            if (productSupplier == null)
            {
                return BadRequest(new { error = "Product not found for this supplier" });
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
            var skusToLink = new List<ProductSkuSellerDomain>();
            var skusToUpdate = new List<ProductSkuSellerDomain>();
            var skusToSkip = new List<ProductSkuSellerDomain>();

            // Criar vínculos para todos os SKUs (apenas os novos)
            foreach (var mapping in skus)
            {
                var skuMapping = request.SkuMappings.FirstOrDefault(s => s.Sku == mapping.Sku);
                var price = skuMapping?.Price ?? request.Price;

                // Verificar se já existe vínculo do seller com este SKU
                var existingSku = await productSkuSellerRepository.GetProductSkuSellerAsync(
                    productId, 
                    mapping.Sku, 
                    sellerId, 
                    "shopee");

                if (existingSku != null)
                {
                    if (existingSku.SupplierId == supplierId)
                    {
                        skusToSkip.Add(existingSku);
                    }
                    else
                    {
                        // Se já existe, adicionar à lista de SKUs para atualizar supplier
                        logger.LogInformation(
                            "SKU already linked to seller, will update supplier - ProductId: {ProductId}, SKU: {Sku}",
                            productId, mapping.Sku);
                        skusToUpdate.Add(existingSku);
                    }
                }
                else if(existingSku == null)
                {
                    // Se não existe, criar novo vínculo
                    var domain = ProductSkuSellerFactory.Create(
                        productId,
                        mapping.Sku,
                        sellerId,
                        "shopee",
                        seller.ShopId,
                        price,
                        mapping.Color,
                        mapping.Size,
                        supplierId
                    );
                    skusToLink.Add(domain);
                }
            }

            // Atualizar supplier_id nos SKUs que já existem (PUT)
            var updatedExistingSkus = new List<ProductSkuSellerDomain>();
            foreach (var sku in skusToUpdate)
            {
                var updated = await productSkuSellerRepository.UpdateSupplierAsync(sku, supplierId);
                if (updated != null)
                {
                    updatedExistingSkus.Add(updated);
                }
            }

            // Vincular os SKUs novos (que ainda não tinham vínculo)
            var linkedRecords = new List<ProductSkuSellerDomain>();
            if (skusToLink.Count > 0)
            {
                linkedRecords = await productSkuSellerRepository.LinkSellerToProductAsync(skusToLink);
            }

            // Combinar SKUs novos com os atualizados
            var allProcessedSkus = linkedRecords.Concat(updatedExistingSkus).ToList();
            allProcessedSkus = allProcessedSkus.Concat(skusToSkip).ToList();

            // Criar registro META para busca rápida de produtos por vendedor (se não existir)
            var productSeller = await productSellerRepository.GetProductSeller(sellerId, "shopee", productId);
            
            if (productSeller == null)
            {
                productSeller = await productSellerRepository.CreateProductSellerAsync(
                    productId,
                    product.Name,
                    sellerId,
                    "shopee",
                    seller.ShopId,
                    request.Price,
                    supplierId,
                    allProcessedSkus.Count
                );
                
                logger.LogInformation(
                    "Seller linked to product successfully - ProductId: {ProductId}, SellerId: {SellerId}, SKUs: {SkuCount}",
                    productId, sellerId, allProcessedSkus.Count);

                await shopeeService.UploadProduct(productSeller, allProcessedSkus);
            }
            else
            {
                // Atualizar registro META existente (PUT)
                logger.LogInformation(
                    "Updating ProductSeller supplier - ProductId: {ProductId}, SellerId: {SellerId}, NewSupplierId: {SupplierId}",
                    productId, sellerId, supplierId);

                productSeller.SupplierId = supplierId;
                productSeller.Price = request.Price;
                productSeller.SkuCount = allProcessedSkus.Count;
                productSeller.UpdatedAt = DateTime.UtcNow;

                await productSellerRepository.Update(productSeller);
            }
            
            return CreatedAtAction(
                nameof(GetSkusBySellerInProduct),
                new { productId },
                allProcessedSkus.ToListResponse()
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
    /// <param name="supplierId"></param>
    /// <param name="request">Novo preço</param>
    /// <returns>Lista de SKUs com preço atualizado</returns>
    [HttpPut("products/{productId}/suppliers/{supplierId}")]
    [ProducesResponseType(typeof(ProductSkuSellerListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSellerProduct(
        [FromRoute] string productId,
        [FromRoute] string supplierId,
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

            var productSeller = await productSellerRepository.GetProductSeller(sellerId, "shopee", productId);
            if (productSeller == null)
            {
                logger.LogWarning("Seller not linked to product - ProductId: {ProductId}, SellerId: {SellerId}", productId, sellerId);
                return NotFound(new { error = "Seller not linked to this product" });
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
                var updated = await productSkuSellerRepository.UpdatePriceAsync(sku, request.Price);
                
                if (updated != null)
                {
                    updatedSkus.Add(updated);
                }
            }
            await shopeeService.UpdatePrice(skus, request.Price);
            
            if (updatedSkus.Count == 0)
            {
                logger.LogWarning("Failed to update any SKUs - ProductId: {ProductId}", productId);
                return NotFound(new { error = "Failed to update product SKUs" });
            }

            productSeller.Price = request.Price;
            await productSellerRepository.Update(productSeller);
            
            logger.LogInformation("Seller product price updated - ProductId: {ProductId}, SellerId: {SellerId}, UpdatedSKUs: {Count}", productId, sellerId, updatedSkus.Count);

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

            var skuItem = await productSkuSellerRepository.GetProductSkuSellerAsync(productId, sku, sellerId, "shopee");
            
            if (skuItem == null)
            {
                logger.LogWarning("SKU not found for seller - ProductId: {ProductId}, SKU: {Sku}, SellerId: {SellerId}", productId, sku, sellerId);
                return NotFound(new { error = "SKU not found for this seller in this product" });
            }
            
            var updated = await productSkuSellerRepository.UpdatePriceAsync(skuItem, request.Price);

            var skuSeller = await productSkuSellerRepository.GetProductSkuSellerAsync(productId, sku, sellerId, "shopee");
            
            await shopeeService.UpdatePrice(skuSeller, request.Price);
            
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
    /// <param name="supplierId"></param>
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

            var product = await productSellerRepository.GetProductSeller(sellerId, "shopee", productId);
            
            if (product is null)
            {
                return NotFound(new { error = "Seller not linked to this product" });
            }
            // Obter todos os SKUs do vendedor neste produto
            var sellerSkus = await productSkuSellerRepository.GetSkusBySellerAsync(productId, sellerId);
            
            // Remover cada SKU do vendedor
            foreach (var sellerSku in sellerSkus)
            {
                await productSkuSellerRepository.RemoveSellerFromSkuAsync(productId, sellerSku.Sku, sellerId, "shopee");
            }

            // Remover registro META do vendedor no produto
            await productSellerRepository.RemoveProductSellerAsync(sellerId, "shopee", productId);

            logger.LogInformation("Seller removed successfully from product - ProductId: {ProductId}, SellerId: {SellerId}, RemovedSKUs: {Count}",
                productId, sellerId, sellerSkus.Count);

            await shopeeApiService.DeleteItemAsync(product.StoreId, product.MarketplaceItemId);
            
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
            var products = await productSupplierRepository.GetAllProductsWithSupplier();
            
            return Ok(products.ToListResponse());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting SKUs for product");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém lista de pagamentos do vendedor
    /// Retorna os pagamentos como estão, sem agrupamento ou consolidação
    /// </summary>
    /// <param name="status">Filtro opcional por status: pending, paid, failed</param>
    /// <returns>Lista de pagamentos</returns>
    [HttpGet("payments/summary")]
    [ProducesResponseType(typeof(List<PaymentQueueDomain>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaymentsSummary([FromQuery] string? status = null)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Getting payments for seller - SellerId: {SellerId}, Status: {Status}", 
            sellerId, status ?? "all");

        try
        {
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Seller ID not found in claims");
                return BadRequest(new { error = "Seller ID not found in authentication claims" });
            }

            // Obter pagamentos do vendedor via service
            List<PaymentQueueDomain> payments;
            if (!string.IsNullOrWhiteSpace(status))
            {
                payments = await paymentService.GetPaymentsBySellerAndStatus(sellerId, status);
            }
            else
            {
                payments = await paymentService.GetPaymentsBySellerId(sellerId);
            }

            if (payments == null || payments.Count == 0)
            {
                logger.LogInformation("No payments found for seller - SellerId: {SellerId}", sellerId);
                return Ok(new List<PaymentQueueDomain>());
            }

            logger.LogInformation("Returning {Count} payments - SellerId: {SellerId}", payments.Count, sellerId);

            return Ok(payments);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payments - SellerId: {SellerId}", sellerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Obtém detalhes de um pagamento específico incluindo lista de produtos
    /// </summary>
    /// <param name="paymentId">ID do pagamento</param>
    /// <returns>Detalhes do pagamento com lista de produtos</returns>
    [HttpGet("payments/{paymentId}")]
    [ProducesResponseType(typeof(PaymentQueueDomain), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentDetail(string paymentId)
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("Getting payment details - PaymentId: {PaymentId}, SellerId: {SellerId}",
            paymentId, sellerId);

        try
        {
            if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(sellerId))
            {
                return BadRequest(new { error = "Payment ID and Seller ID are required" });
            }

            // Obter todos os pagamentos do vendedor via service
            var allPayments = await paymentService.GetPaymentsBySellerId(sellerId);

            if (allPayments == null || allPayments.Count == 0)
            {
                logger.LogWarning("No payments found for seller - SellerId: {SellerId}", sellerId);
                return NotFound(new { error = "Payment not found" });
            }

            // Procurar o pagamento pelo PaymentId
            var payment = allPayments.FirstOrDefault(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                logger.LogWarning("Payment not found - PaymentId: {PaymentId}, SellerId: {SellerId}", paymentId, sellerId);
                return NotFound(new { error = "Payment not found" });
            }

            logger.LogInformation("Returning payment details - PaymentId: {PaymentId}", paymentId);

            return Ok(payment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payment details - PaymentId: {PaymentId}", paymentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Cria um link de pagamento para InfinityPay
    /// </summary>
    /// <param name="request">Array de paymentIds e valor total</param>
    /// <returns>LinkId e URL de checkout</returns>
    [HttpPost("payments/create-link")]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreateInfinityPayLinkRequest request)
    {
        try
        {
            var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
            
            logger.LogInformation(
                "Creating InfinityPay link - SellerId: {SellerId}, PaymentIds: {Count}",
                sellerId, request.PaymentIds.Count);

            // Validar campos obrigatórios
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                logger.LogWarning("Missing seller ID in request");
                return BadRequest(new { error = "Seller ID is required (X-Seller-Id header)" });
            }

            if (request.PaymentIds == null || request.PaymentIds.Count == 0)
            {
                logger.LogWarning("No payment IDs provided - SellerId: {SellerId}", sellerId);
                return BadRequest(new { error = "At least one payment ID is required" });
            }

            if (request.Amount <= 0)
            {
                logger.LogWarning("Invalid amount - SellerId: {SellerId}", sellerId);
                return BadRequest(new { error = "Amount must be greater than 0" });
            }

            var link = await paymentService.CreateInfinityPayLinkAsync(
                sellerId: sellerId,
                paymentIds: request.PaymentIds,
                totalAmount: request.Amount);


            logger.LogInformation(
                "InfinityPay link created successfully - LinkId: {LinkId}, SellerId: {SellerId}",
                link.LinkId, sellerId);

            return Ok(link);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Validation error creating InfinityPay link");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating InfinityPay link");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
    /// <summary>
    /// Obtém informações de um link de pagamento
    /// </summary>
    /// <param name="linkId">ID do link (ULID)</param>
    [HttpGet("links/{linkId}")]
    public async Task<IActionResult> GetLink(string linkId)
    {
        try
        {
            logger.LogInformation("Getting InfinityPay link - LinkId: {LinkId}", linkId);

            var link = await linkRepository.GetLinkByIdAsync(linkId);
            
            if (link == null)
            {
                logger.LogWarning("Link not found - LinkId: {LinkId}", linkId);
                return NotFound(new { error = "Link not found" });
            }

            return Ok(link);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting InfinityPay link - LinkId: {LinkId}", linkId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gera e retorna a etiqueta de envio de um pedido em PDF para download.
    ///
    /// Fluxo interno:
    /// 1. Busca o pedido no banco de dados pelo orderId (= orderSn)
    /// 2. Usa o shopId do pedido para autenticar na Shopee
    /// 3. Chama create_shipping_document na Shopee
    /// 4. Aguarda o documento ficar READY via get_shipping_document_result (polling)
    /// 5. Faz o download e retorna o PDF diretamente
    /// </summary>
    /// <param name="orderId">Número do pedido (orderSn)</param>
    /// <param name="documentType">Tipo da etiqueta: THERMAL_AIR_WAYBILL (default), A4_AIR_WAYBILL, OFFICIAL_AIR_WAYBILL</param>
    [HttpGet("orders/{orderId}/print")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PrintShippingLabel(
        string orderId,
        [FromQuery] string documentType = "THERMAL_AIR_WAYBILL")
    {
        var sellerId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "resourceId")?.Value;
        logger.LogInformation("PrintShippingLabel - OrderId: {OrderId}, SellerId: {SellerId}, DocumentType: {DocumentType}",
            orderId, sellerId, documentType);

        try
        {
            if (string.IsNullOrWhiteSpace(sellerId))
                return BadRequest(new { error = "Seller ID not found in authentication claims" });

            if (string.IsNullOrWhiteSpace(orderId))
                return BadRequest(new { error = "orderId is required" });

            // 1. Buscar pedido no banco para obter o shopId
            var order = await orderRepository.GetOrderBySnAsync(sellerId, orderId);
            if (order == null)
            {
                logger.LogWarning("Order not found - OrderId: {OrderId}, SellerId: {SellerId}", orderId, sellerId);
                return NotFound(new { error = "Order not found" });
            }

            var shopId = order.ShopId;
            var orderList = new[] { new { order_sn = orderId } };

            logger.LogInformation("Creating shipping document - OrderId: {OrderId}, ShopId: {ShopId}", orderId, shopId);

            // 2. Criar o documento de envio na Shopee
            await shopeeApiService.CreateShippingDocumentAsync(shopId, orderList);

            // 3. Polling até o documento ficar READY (máx 10 tentativas com 2s de intervalo)
            const int maxRetries = 10;
            const int delayMs = 2000;
            bool isReady = false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                logger.LogInformation("Checking document status - Attempt: {Attempt}/{Max}, OrderId: {OrderId}",
                    attempt, maxRetries, orderId);

                var resultDoc = await shopeeApiService.GetShippingDocumentResultAsync(shopId, orderList, documentType);

                var resultJson = JObject.Parse(resultDoc.RootElement.GetRawText());
                var resultList = resultJson["response"]?["result_list"] as JArray;

                if (resultList?.Count > 0)
                {
                    var first = resultList[0];
                    var status = first["status"]?.Value<string>();

                    logger.LogInformation("Document status: {Status} - OrderId: {OrderId}", status, orderId);

                    if (status == "READY")
                    {
                        isReady = true;
                        break;
                    }

                    if (status == "FAILED")
                    {
                        var failMsg = first["fail_message"]?.Value<string>() ?? "Unknown error";
                        logger.LogWarning("Document generation failed - OrderId: {OrderId}, Message: {Msg}", orderId, failMsg);
                        return StatusCode(StatusCodes.Status502BadGateway, new { error = $"Shopee failed to generate document: {failMsg}" });
                    }
                }

                if (attempt < maxRetries)
                    await Task.Delay(delayMs);
            }

            if (!isReady)
            {
                logger.LogWarning("Document not ready after {MaxRetries} attempts - OrderId: {OrderId}", maxRetries, orderId);
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    new { error = "Shipping document not ready. Please try again in a few seconds." });
            }

            // 4. Download da etiqueta
            logger.LogInformation("Downloading shipping label - OrderId: {OrderId}, ShopId: {ShopId}", orderId, shopId);
            var (fileBytes, contentType) = await shopeeApiService.DownloadShippingDocumentAsync(shopId, orderList, documentType);

            var fileName = $"etiqueta_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

            logger.LogInformation("Shipping label ready for download - OrderId: {OrderId}, Bytes: {Bytes}, FileName: {FileName}",
                orderId, fileBytes.Length, fileName);

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error printing shipping label - OrderId: {OrderId}, SellerId: {SellerId}", orderId, sellerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}