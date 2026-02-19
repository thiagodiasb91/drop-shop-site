using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

public class ProductSupplierRepository(DynamoDbRepository repository,
                                      ProductRepository productRepository,
    ILogger<ProductSupplierRepository> logger)
{
    
    public async Task CreateProductSupplierAsync(
        string productId,
        string supplierId,
        string productName,
        List<ProductSkuSupplierDomain>? skus )
    {
        logger.LogInformation("Creating product-supplier link - ProductId: {ProductId}, SupplierId: {SupplierId}", productId, supplierId);

        try
        {
            var createdAtUtc = DateTime.UtcNow.ToString("O"); // ISO 8601 format

            // Calcular preços min/max a partir dos SKUs
            decimal minPrice = 0;
            decimal maxPrice = 0;

            if (skus is { Count: > 0 })
            {
                var validPrices = skus.Where(s => s.Price > 0).Select(s => s.Price).ToList();
                if (validPrices.Count > 0)
                {
                    minPrice = validPrices.Min();
                    maxPrice = validPrices.Max();
                }
            }

            var record = new ProductSupplierDomain
            {
                Pk = $"Supplier#{supplierId}",
                Sk = $"Product#{productId}",
                EntityType = "product_supplier",
                ProductId = productId,
                ProductName = productName,
                SupplierId = supplierId,
                SkuCount = skus.Count,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                CreatedAt = DateTime.UtcNow
            };

            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = record.Pk } },
                { "SK", new AttributeValue { S = record.Sk } },
                { "entity_type", new AttributeValue { S = record.EntityType } },
                { "product_id", new AttributeValue { S = record.ProductId } },
                { "product_name", new AttributeValue { S = record.ProductName } },
                { "supplier_id", new AttributeValue { S = record.SupplierId } },
                { "sku_count", new AttributeValue { N = record.SkuCount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { "min_price", new AttributeValue { N = minPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) } },
                { "max_price", new AttributeValue { N = maxPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) } },
                { "created_at", new AttributeValue { S = createdAtUtc } }
            };

            await repository.PutItemAsync(item);

            logger.LogInformation("Product-supplier link created - ProductId: {ProductId}, SupplierId: {SupplierId}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}", 
                productId, supplierId, minPrice, maxPrice);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product-supplier link - ProductId: {ProductId}, SupplierId: {SupplierId}", productId, supplierId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os fornecedores de um produto específico
    /// Faz scan na tabela filtrando por product_id e entity_type = "product_supplier"
    /// </summary>
    public async Task<List<ProductSupplierDomain>> GetSuppliersByProductIdAsync(string productId)
    {
        logger.LogInformation("Getting suppliers for product - ProductId: {ProductId}", productId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":product_id", new AttributeValue { S = $"Product#{productId}" } },
                { ":pk", new AttributeValue() { S = "Supplier#"}}
            };

            var items = await repository.QueryTableAsync(
                keyConditionExpression: "SK = :product_id AND begins_with(PK, :pk)",
                expressionAttributeValues: expressionAttributeValues,
                indexName:"GSI_RELATIONS_LOOKUP"    
            );

            if (items == null || items.Count == 0)
            {
                logger.LogDebug("No suppliers found for product - ProductId: {ProductId}", productId);
                return [];
            }

            var suppliers = items
                .Select( ProductSupplierMapper.ToDomain)
                .ToList();

            logger.LogInformation("Found {Count} suppliers for product - ProductId: {ProductId}",
                suppliers.Count, productId);

            return suppliers;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting suppliers for product - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os produtos fornecidos por um fornecedor específico
    /// </summary>
    public async Task<List<ProductSupplierDomain>> GetProductsBySupplierAsync(string supplierId)
    {
        logger.LogInformation("Getting products for supplier - SupplierId: {SupplierId}", supplierId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { ":sk_prefix", new AttributeValue { S = "Product#" } }
            };

            var items = await repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_prefix)",
                expressionAttributeValues: expressionAttributeValues
            );

            if (items == null || items.Count == 0)
            {
                logger.LogDebug("No products found for supplier - SupplierId: {SupplierId}", supplierId);
                return new List<ProductSupplierDomain>();
            }

            var products = items
                .Select(ProductSupplierMapper.ToDomain)
                .ToList();

            logger.LogInformation("Found {Count} products for supplier - SupplierId: {SupplierId}",
                products.Count, supplierId);

            return products;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products for supplier - SupplierId: {SupplierId}", supplierId);
            throw;
        }
    }
    
    /// <summary>
    /// Obtém todos os produtos fornecidos por um fornecedor específico
    /// </summary>
    public async Task<ProductSupplierDomain?> GetProductBySupplier(string supplierId, string productId)
    {
        logger.LogInformation("Getting products for supplier - SupplierId: {SupplierId} - ProductId: {ProductId}", supplierId, productId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { ":sk", new AttributeValue { S = $"Product#{productId}" } }
            };

            var items = await repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND SK = :sk",
                expressionAttributeValues: expressionAttributeValues
            );

            if (items == null || items.Count == 0)
            {
                logger.LogDebug("No products found for supplier - SupplierId: {SupplierId} - ProductId {ProductId}", supplierId, productId);
                return null;
            }

            var products = items
                .Select(ProductSupplierMapper.ToDomain)
                .ToList();

            logger.LogInformation("Found {Count} products for supplier - SupplierId: {SupplierId}",
                products.Count, supplierId);

            return products.First();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products for supplier - SupplierId: {SupplierId}", supplierId);
            throw;
        }
    }

    /// <summary>
    /// Remove um produto da lista de produtos fornecidos por um fornecedor
    /// </summary>
    public async Task RemoveProductSupplierAsync(string supplierId, string productId)
    {
        logger.LogInformation("Removing product from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
            supplierId, productId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            await repository.DeleteItemAsync(key);

            logger.LogInformation("Product removed from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing product from supplier - SupplierId: {SupplierId}, ProductId: {ProductId}",
                supplierId, productId);
            throw;
        }
    }

    public async Task<bool> HasSupplierRelation(string productId)
    {
        logger.LogInformation("Checking if product has supplier relation - ProductId: {ProductId}", productId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = "Supplier#" } },
                { ":sk", new AttributeValue { S = $"Product#{productId}" } }
            };

            var items = await repository.QueryTableAsync(
                indexName: "GSI_RELATIONS",
                keyConditionExpression: "SK = :sk AND begins_with(PK, :pk)",
                expressionAttributeValues: expressionAttributeValues
            );

            return items != null && items.Count > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing product from supplier - ProductId: {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os produtos que possuem pelo menos um fornecedor vinculado
    /// Usa foreach assíncrono para verificar relações em paralelo quando possível
    /// </summary>
    /// <returns>Lista de produtos que têm fornecedores vinculados</returns>
    public async Task<List<ProductDomain>> GetAllProductsWithSupplier()
    {
        logger.LogInformation("Getting all products with supplier relations");

        try
        {
            var products = await productRepository.GetAllProductsAsync();
            var productsWithSupplier = new List<ProductDomain>();

            foreach (var product in products)
            {
                if (!await HasSupplierRelation(product.Id)) continue;
                
                productsWithSupplier.Add(product);
                logger.LogDebug("Product has supplier relation - ProductId: {ProductId}", product.Id);
            }

            logger.LogInformation("Found {Count} products with supplier relations out of {Total} total products",
                productsWithSupplier.Count, products.Count);

            return productsWithSupplier;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all products with supplier relations");
            throw;
        }
    }

    /// <summary>
    /// Atualiza os preços mínimo e máximo do relacionamento product_supplier
    /// baseado nos preços dos SKUs fornecidos pelo fornecedor
    /// </summary>
    public async Task UpdatePricesAsync(
        string productId,
        string supplierId,
        List<ProductSkuSupplierDomain> skus)
    {
        logger.LogInformation("Updating product-supplier prices - ProductId: {ProductId}, SupplierId: {SupplierId}, SKUCount: {SKUCount}",
            productId, supplierId, skus.Count);

        try
        {
            if (skus == null || skus.Count == 0)
            {
                logger.LogWarning("No SKUs found to update prices - ProductId: {ProductId}, SupplierId: {SupplierId}",
                    productId, supplierId);
                return;
            }

            // Obter preços válidos (maior que 0)
            var validPrices = skus
                .Where(s => s.Price > 0)
                .Select(s => s.Price)
                .ToList();

            if (validPrices.Count == 0)
            {
                logger.LogWarning("No valid prices found in SKUs - ProductId: {ProductId}, SupplierId: {SupplierId}",
                    productId, supplierId);
                return;
            }

            var minPrice = validPrices.Min();
            var maxPrice = validPrices.Max();

            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
                { "SK", new AttributeValue { S = $"Product#{productId}" } }
            };

            var updateExpression = "SET min_price = :minPrice, max_price = :maxPrice, updated_at = :updatedAt";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":minPrice", new AttributeValue { N = minPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) } },
                { ":maxPrice", new AttributeValue { N = maxPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) } },
                { ":updatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("O") } }
            };

            await repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            logger.LogInformation("Product-supplier prices updated - ProductId: {ProductId}, SupplierId: {SupplierId}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}",
                productId, supplierId, minPrice, maxPrice);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product-supplier prices - ProductId: {ProductId}, SupplierId: {SupplierId}",
                productId, supplierId);
            throw;
        }
    }
}

