using Dropship.Repository;

namespace Dropship.Services;

public class StockServices (ILogger<StockServices> logger,
                            ProductSkuSellerRepository productSkuSellerRepository,
                            ShopeeApiService shopeeApiService)
{
    public async Task UpdateStockSupplier(string supplierId, string productId, string sku, int quantity)
    {
        logger.LogInformation("Updating stock for supplier {SupplierId}, product {ProductId}, sku {Sku} with quantity {Quantity}", supplierId, productId, sku, quantity);
        
        var productsToUpdate = await productSkuSellerRepository.GetProductSkuSellerBySupplier(productId, sku, "shopee", supplierId);

        logger.LogInformation("Found {Count} products to update for supplier {SupplierId}, product {ProductId}, sku {Sku}", productsToUpdate.Count, supplierId, productId, sku);

        if (productsToUpdate == null) return;
        
        foreach (var productToUpdate in productsToUpdate)
        {
            logger.LogInformation("Updating stock for product {ProductId}, sku {Sku} in store {StoreId} with quantity {Quantity}", productToUpdate.ProductId, productToUpdate.Sku, productToUpdate.StoreId, quantity);
            
            await shopeeApiService.UpdateStockAsync(productToUpdate.StoreId, long.Parse(productToUpdate.MarketplaceItemId), long.Parse(productToUpdate.MarketplaceModelId), quantity);
            await productSkuSellerRepository.UpdateStockAsync(productToUpdate, quantity);
        }
    }
}