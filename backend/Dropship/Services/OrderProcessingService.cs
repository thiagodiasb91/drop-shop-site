using Dropship.Domain;
using Dropship.Repository;
using Newtonsoft.Json.Linq;

namespace Dropship.Services;

public class OrderProcessingService(
    ShopeeApiService shopeeApiService,
    KardexService kardexService,
    PaymentRepository paymentRepository,
    SellerRepository sellerRepository,
    ProductSkuSellerRepository productSkuSellerRepository,
    SkuRepository skuRepository,
    ProductSkuSupplierRepository productSkuSupplierRepository,
    ILogger<OrderProcessingService> logger)
{
    /// <summary>
    /// Processa um pedido realizado na Shopee
    /// Agrupa itens por fornecedor e cria registros de pagamento consolidados
    /// </summary>
    public async Task<bool> ProcessOrderAsync(string orderSn, string status, long shopId)
    {
        logger.LogInformation("Processing order - OrderSn: {OrderSn}, Status: {Status}, ShopId: {ShopId}", 
            orderSn, status, shopId);

        try
        {
            // Validar se status é "READY_TO_SHIP"
            if (status != "READY_TO_SHIP")
            {
                logger.LogWarning("Order status is not READY_TO_SHIP - OrderSn: {OrderSn}, Status: {Status}", 
                    orderSn, status);
                return false;
            }

            // Obter detalhes do pedido via API Shopee
            var orderDetail = await shopeeApiService.GetOrderDetailAsync(shopId, [orderSn]);

            // Parse response com Newtonsoft.Json
            var responseJson = orderDetail.RootElement.GetRawText();
            var jObject = JObject.Parse(responseJson);
            
            var response = jObject["response"] ?? throw new InvalidOperationException("Invalid response structure");
            var orderList = response["order_list"] ?? throw new InvalidOperationException("No orders found in response");
            
            if (!orderList.Any())
            {
                logger.LogError("Empty order list in response - OrderSn: {OrderSn}", orderSn);
                return false;
            }

            var order = orderList.First();
            var itemList = order["item_list"] ?? throw new InvalidOperationException("No items found in order");

            if (!itemList.HasValues)
            {
                logger.LogError("Empty item list in order - OrderSn: {OrderSn}", orderSn);
                return false;
            }

            logger.LogInformation("Processing {Count} items in order - OrderSn: {OrderSn}", 
                itemList.Count(), orderSn);

            // Agrupar itens por fornecedor
            var itemsBySupplier = new Dictionary<string, List<OrderItemData>>();

            foreach (var item in itemList)
            {
                var modelSku = item["model_sku"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(modelSku))
                {
                    logger.LogWarning("Item has no model_sku - OrderSn: {OrderSn}", orderSn);
                    continue;
                }

                var qtyString = item["model_quantity_purchased"]?.Value<string>();
                if (!int.TryParse(qtyString, out var quantityPurchased))
                {
                    logger.LogWarning("Invalid quantity format - OrderSn: {OrderSn}, SKU: {SKU}", 
                        orderSn, modelSku);
                    continue;
                }

                var price = item["model_original_price"]?.Value<decimal>() ?? 0;
                var productId = await skuRepository.GetProductIdBySkuAsync(modelSku);
                
                if (string.IsNullOrEmpty(productId))
                {
                    logger.LogWarning("Product not found for SKU - SKU: {SKU}", modelSku);
                    continue;
                }

                var seller = await sellerRepository.GetSellerByShopIdAsync(shopId);
                if (seller == null)
                {
                    logger.LogError("Seller not found for shop - ShopId: {ShopId}", shopId);
                    throw new InvalidOperationException($"Seller not found for shop ID {shopId}");
                }

                var skuSeller = await productSkuSellerRepository.GetProductSkuSellerAsync(
                    productId, modelSku, seller.SellerId, "shopee");
                var skuSupplier = await productSkuSupplierRepository.GetProductSkuSupplierAsync(
                    productId, modelSku, skuSeller.SupplierId);

                // Agrupar por SupplierId
                if (!itemsBySupplier.ContainsKey(skuSeller.SupplierId))
                {
                    itemsBySupplier[skuSeller.SupplierId] = new List<OrderItemData>();
                }

                itemsBySupplier[skuSeller.SupplierId].Add(new OrderItemData
                {
                    ProductId = productId,
                    Sku = modelSku,
                    Quantity = quantityPurchased,
                    Price = price,
                    ProductionPrice = skuSupplier.Price,
                    SupplierId = skuSeller.SupplierId,
                    Seller = seller,
                    Image = item["image_info"]["image_url"].Value<string>()
                });
            }

            // Processar cada fornecedor com seus itens consolidados
            foreach (var supplierGroup in itemsBySupplier)
            {
                var supplierId = supplierGroup.Key;
                var items = supplierGroup.Value;

                await ProcessSupplierPaymentAsync(
                    supplierId: supplierId,
                    orderSn: orderSn,
                    shopId: shopId,
                    items: items);
            }

            logger.LogInformation("Order processed successfully - OrderSn: {OrderSn}", orderSn);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing order - OrderSn: {OrderSn}, ShopId: {ShopId}", 
                orderSn, shopId);
            throw;
        }
    }

    /// <summary>
    /// Processa o pagamento consolidado de um fornecedor
    /// Atualiza estoque, registra Kardex e cria um único registro de pagamento consolidado
    /// </summary>
    private async Task ProcessSupplierPaymentAsync(
        string supplierId,
        string orderSn,
        long shopId,
        List<OrderItemData> items)
    {
        logger.LogInformation(
            "Processing supplier payment - SupplierId: {SupplierId}, OrderSn: {OrderSn}, ItemCount: {ItemCount}",
            supplierId, orderSn, items.Count);

        try
        {
            var seller = items.First().Seller;

            logger.LogInformation(
                "Found supplier items - SupplierId: {SupplierId}, OrderSn: {OrderSn}, SellerId: {SellerId}, ItemCount: {ItemCount}",
                supplierId, orderSn, seller.SellerId, items.Count);

            // Atualizar estoque do fornecedor para cada SKU
            foreach (var item in items)
            {
                await productSkuSupplierRepository.UpdateSupplierStockAsync(
                    productId: item.ProductId,
                    sku: item.Sku,
                    supplierId: supplierId,
                    quantity: item.Quantity);

                // Adicionar ao Kardex para cada SKU
                await kardexService.AddToKardexAsync(
                    productId: item.ProductId,
                    sku: item.Sku,
                    quantity: item.Quantity,
                    orderSn: orderSn,
                    shopId: shopId,
                    supplierId: supplierId);
            }

            var paymentProducts = items.Select(i => new PaymentProduct
            {
                ProductId = i.ProductId,
                Sku = i.Sku,
                Quantity = i.Quantity,
                UnitPrice = i.ProductionPrice,
                Image = i.Image
            }).ToList();
            
            var paymentQueue = PaymentQueueBuilder.Create(
                sellerId: seller.SellerId,
                supplierId: supplierId,
                orderSn: orderSn,
                shopId: shopId,
                products: paymentProducts
            );

            await paymentRepository.CreatePaymentQueueAsync(paymentQueue);

            logger.LogInformation(
                "Supplier payment processed - SupplierId: {SupplierId}, OrderSn: {OrderSn}, ConsolidatedItems: {Count}",
                supplierId, orderSn, items.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error processing supplier payment - SupplierId: {SupplierId}, OrderSn: {OrderSn}",
                supplierId, orderSn);
            throw;
        }
    }

    /// <summary>
    /// Classe auxiliar para armazenar dados do item durante o processamento
    /// </summary>
    private class OrderItemData
    {
        public string ProductId { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ProductionPrice { get; set; }
        public string SupplierId { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public SellerDomain Seller { get; set; } = default!;
    }
}






