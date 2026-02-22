using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Microsoft.Extensions.Logging;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com Kardex no DynamoDB
/// Gerencia registros de movimentação de estoque
/// </summary>
public class KardexRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<KardexRepository> _logger;

    public KardexRepository(DynamoDbRepository repository, ILogger<KardexRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todos os registros de Kardex para um SKU específico
    /// </summary>
    public async Task<List<KardexDomain>> GetKardexBySkuAsync(string sku)
    {
        _logger.LogInformation("Getting kardex by SKU - SKU: {SKU}", sku);

        try
        {
            var keyConditionExpression = "PK = :pk";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"Kardex#Sku#{sku}" } }
            };

            var items = await _repository.QueryTableAsync(
                keyConditionExpression,
                expressionAttributeValues: expressionAttributeValues
            );

            var kardexList = items.Select(KardexMapper.ToDomain).ToList();
            
            _logger.LogInformation("Found {Count} kardex entries for SKU - SKU: {SKU}", 
                kardexList.Count, sku);
            
            return kardexList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting kardex by SKU - SKU: {SKU}", sku);
            throw;
        }
    }

    /// <summary>
    /// Adiciona um novo registro ao Kardex
    /// Registra operações de movimentação de estoque (add, remove, etc)
    /// </summary>
    public async Task<KardexDomain> AddToKardexAsync(KardexDomain kardex)
    {
        _logger.LogInformation("Adding to kardex - ProductId: {ProductId}, SKU: {SKU}, Operation: {Operation}, Quantity: {Quantity}",
            kardex.ProductId, kardex.SK, kardex.Operation, kardex.Quantity);

        try
        {
            // Validação básica
            if (string.IsNullOrWhiteSpace(kardex.ProductId))
                throw new ArgumentException("ProductId is required");

            if (string.IsNullOrWhiteSpace(kardex.SK))
                throw new ArgumentException("SKU is required");

            if (string.IsNullOrWhiteSpace(kardex.Operation))
                throw new ArgumentException("Operation is required");

            // Gerar ID único para o Kardex
            var kardexId = Ulid.NewUlid().ToString();

            // Montar o item para DynamoDB
            var item = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"Kardex#Sku#{kardex.SK}" } },
                { "SK", new AttributeValue { S = kardexId } },
                { "product_id", new AttributeValue { S = kardex.ProductId } },
                { "sku", new AttributeValue { S = kardex.SK } },
                { "entity_type", new AttributeValue { S = "kardex" } },
                { "quantity", new AttributeValue { N = kardex.Quantity.ToString() } },
                { "operation", new AttributeValue { S = kardex.Operation } },
                { "date", new AttributeValue { S = DateTime.UtcNow.ToString("O") } },
                { "supplier_id", new AttributeValue { S = kardex.SupplierId } }
            };

            // Adicionar opcionais se disponíveis
            if (!string.IsNullOrWhiteSpace(kardex.OrderSn))
            {
                item["ordersn"] = new AttributeValue { S = kardex.OrderSn };
            }

            if (kardex.ShopId.HasValue && kardex.ShopId > 0)
            {
                item["shop_id"] = new AttributeValue { N = kardex.ShopId.Value.ToString() };
            }

            await _repository.PutItemAsync(item);

            // Retornar o kardex com o ID gerado
            kardex.SK = kardexId;
            kardex.Date = DateTime.UtcNow.ToString("O");

            _logger.LogInformation("Kardex entry added successfully - ProductId: {ProductId}, KardexId: {KardexId}",
                kardex.ProductId, kardexId);

            return kardex;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to kardex - ProductId: {ProductId}, Operation: {Operation}",
                kardex.ProductId, kardex.Operation);
            throw;
        }
    }
}