using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para gerenciar relações entre Produtos, SKUs e Fornecedores
/// Permite vincular fornecedores com produtos e seus SKUs com preço de produção
/// </summary>
public class ProductSkuSupplierRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<ProductSkuSupplierRepository> _logger;

    public ProductSkuSupplierRepository(DynamoDbRepository repository, ILogger<ProductSkuSupplierRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Vincula um fornecedor a um produto com preço de produção
    /// Cria um registro para cada SKU do produto
    /// </summary>
    /// <param name="productId">ID do produto</param>
    /// <param name="supplierId">ID do fornecedor</param>
    /// <param name="productionPrice">Preço de produção</param>
    /// <param name="priority">Prioridade de fornecimento (opcional)</param>
    /// <param name="skus">Lista de SKUs do produto</param>
    public async Task<List<ProductSkuSupplierDomain>> LinkSupplierToProductAsync(
        string productId,
        string supplierId,
        decimal productionPrice,
        List<string> skus)
    {
        _logger.LogInformation("Linking supplier to product - ProductId: {ProductId}, SupplierId: {SupplierId}, SKUs: {SkuCount}",
            productId, supplierId, skus.Count);

        var createdRecords = new List<ProductSkuSupplierDomain>();

        try
        {
            // Criar um registro para cada SKU
            foreach (var sku in skus)
            {
                var record = await CreateProductSkuSupplierAsync(productId, sku, supplierId, productionPrice);
                createdRecords.Add(record);

                _logger.LogDebug("Created product-sku-supplier link - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
                    productId, sku, supplierId);
            }

            _logger.LogInformation("Successfully linked {Count} SKUs to supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
                createdRecords.Count, productId, supplierId);

            return createdRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking supplier to product - ProductId: {ProductId}, SupplierId: {SupplierId}",
                productId, supplierId);
            throw;
        }
    }

    /// <summary>
    /// Cria um registro individual de Product-SKU-Supplier
    /// </summary>
    private async Task<ProductSkuSupplierDomain> CreateProductSkuSupplierAsync(
        string productId,
        string sku,
        string supplierId,
        decimal productionPrice)
    {
        var record = new ProductSkuSupplierDomain
        {
            Pk = $"Product#{productId}",
            Sk = $"Sku#{sku}#Supplier#{supplierId}",
            EntityType = "product_sku_supplier",
            ProductId = productId,
            Sku = sku,
            SupplierId = supplierId,
            ProductionPrice = productionPrice,
            Quantity = 0
        };

        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = record.Pk } },
            { "SK", new AttributeValue { S = record.Sk } },
            { "entity_type", new AttributeValue { S = record.EntityType } },
            { "product_id", new AttributeValue { S = record.ProductId } },
            { "sku", new AttributeValue { S = record.Sku } },
            { "supplier_id", new AttributeValue { S = record.SupplierId } },
            { "production_price", new AttributeValue { N = record.ProductionPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
            { "quantity", new AttributeValue { N = record.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) } }
        };

        await _repository.PutItemAsync(item);

        return record;
    }

    /// <summary>
    /// Obtém todos os fornecedores de um SKU específico do produto
    /// </summary>
    public async Task<List<ProductSkuSupplierDomain>> GetSuppliersBySku(string productId, string sku)
    {
        _logger.LogInformation("Getting suppliers for SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Product#{productId}" } },
                { ":sk_prefix", new AttributeValue { S = $"Sku#{sku}#Supplier#" } }
            };

            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_prefix)",
                expressionAttributeValues: expressionAttributeValues
            );

            if (items == null || items.Count == 0)
            {
                _logger.LogDebug("No suppliers found for SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
                return new List<ProductSkuSupplierDomain>();
            }

            var suppliers = items
                .Select(ProductSkuSupplierMapper.ToDomain)
                .ToList();

            return suppliers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suppliers for SKU - ProductId: {ProductId}, SKU: {Sku}", productId, sku);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os SKUs fornecidos por um fornecedor específico para um produto
    /// </summary>
    public async Task<List<ProductSkuSupplierDomain>> GetSkusBySupplier(string productId, string supplierId)
    {
        _logger.LogInformation("Getting SKUs supplied by supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
            productId, supplierId);

        try
        {
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Product#{productId}" } },
                { ":sk_pattern", new AttributeValue { S = $"Sku#" } },
                { ":supplier", new AttributeValue { S = supplierId } }
            };

            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk_pattern)",
                expressionAttributeValues: expressionAttributeValues,
                filterExpression: "supplier_id = :supplier"
            );

            if (items == null || items.Count == 0)
            {
                _logger.LogDebug("No SKUs found for supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
                    productId, supplierId);
                return new List<ProductSkuSupplierDomain>();
            }

            var skus = items
                .Select(ProductSkuSupplierMapper.ToDomain)
                .ToList();

            return skus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SKUs for supplier - ProductId: {ProductId}, SupplierId: {SupplierId}",
                productId, supplierId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza o preço de produção e quantidade de um SKU-Supplier específico
    /// </summary>
    public async Task<ProductSkuSupplierDomain?> UpdateSupplierPricingAsync(
        string productId,
        string sku,
        string supplierId,
        decimal productionPrice,
        long quantity)
    {
        _logger.LogInformation("Updating supplier pricing - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
            productId, sku, supplierId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Supplier#{supplierId}" } }
            };

            var updateExpression = "SET production_price = :price, quantity = :qty";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":price", new AttributeValue { N = productionPrice.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                { ":qty", new AttributeValue { N = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) } }
            };

            await _repository.UpdateItemAsync(key, updateExpression, expressionAttributeValues);

            _logger.LogInformation("Supplier pricing updated - ProductId: {ProductId}, SKU: {Sku}, Price: {Price}, Qty: {Qty}",
                productId, sku, productionPrice, quantity);

            // Retornar o registro atualizado
            var item = await _repository.GetItemAsync(key);
            return item != null ? ProductSkuSupplierMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier pricing - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
                productId, sku, supplierId);
            throw;
        }
    }

    /// <summary>
    /// Remove um fornecedor de um SKU específico
    /// </summary>
    public async Task<bool> RemoveSupplierFromSku(string productId, string sku, string supplierId)
    {
        _logger.LogInformation("Removing supplier from SKU - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
            productId, sku, supplierId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Supplier#{supplierId}" } }
            };

            await _repository.DeleteItemAsync(key);

            _logger.LogInformation("Supplier removed from SKU - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
                productId, sku, supplierId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing supplier from SKU - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
                productId, sku, supplierId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um registro específico de Product-SKU-Supplier
    /// </summary>
    public async Task<ProductSkuSupplierDomain?> GetProductSkuSupplierAsync(string productId, string sku, string supplierId)
    {
        _logger.LogInformation("Getting product-sku-supplier record - ProductId: {ProductId}, SKU: {Sku}, SupplierId: {SupplierId}",
            productId, sku, supplierId);

        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Product#{productId}" } },
                { "SK", new AttributeValue { S = $"Sku#{sku}#Supplier#{supplierId}" } }
            };

            var item = await _repository.GetItemAsync(key);
            return item != null ? ProductSkuSupplierMapper.ToDomain(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product-sku-supplier record - ProductId: {ProductId}, SKU: {Sku}",
                productId, sku);
            throw;
        }
    }
}



