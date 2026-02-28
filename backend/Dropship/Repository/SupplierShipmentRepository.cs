using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com SupplierShipment no DynamoDB
/// Gerencia registros de romaneio/envio vinculados a fornecedores
/// </summary>
public class SupplierShipmentRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<SupplierShipmentRepository> _logger;

    public SupplierShipmentRepository(DynamoDbRepository repository, ILogger<SupplierShipmentRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Cria um novo registro de romaneio/envio do fornecedor
    /// </summary>
    public async Task CreateShipmentAsync(SupplierShipmentDomain shipment)
    {
        _logger.LogInformation(
            "Creating supplier shipment - SupplierId: {SupplierId}, PaymentId: {PaymentId}, OrderSn: {OrderSn}",
            shipment.SupplierId, shipment.PaymentId, shipment.OrderSn);

        try
        {
            var item = shipment.ToDynamoDb();

            await _repository.PutItemAsync(item);

            _logger.LogInformation(
                "Supplier shipment created successfully - ShipmentId: {ShipmentId}, PaymentId: {PaymentId}",
                shipment.ShipmentId, shipment.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating supplier shipment - SupplierId: {SupplierId}, PaymentId: {PaymentId}",
                shipment.SupplierId, shipment.PaymentId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os romaneios de um fornecedor
    /// </summary>
    public async Task<List<SupplierShipmentDomain>> GetShipmentsBySupplierAsync(string supplierId)
    {
        _logger.LogInformation("Getting shipments by supplier - SupplierId: {SupplierId}", supplierId);

        try
        {
            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Supplier#{supplierId}" } },
                    { ":sk", new AttributeValue { S = "Shipment#" } }
                }
            );

            var shipments = items.Select(SupplierShipmentMapper.ToDomain).ToList();

            _logger.LogInformation("Found {Count} shipments for supplier - SupplierId: {SupplierId}",
                shipments.Count, supplierId);

            return shipments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shipments by supplier - SupplierId: {SupplierId}", supplierId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um romaneio específico pelo seu ShipmentId
    /// </summary>
    public async Task<SupplierShipmentDomain?> GetShipmentByIdAsync(string supplierId, string shipmentId)
    {
        _logger.LogInformation("Getting shipment by ID - SupplierId: {SupplierId}, ShipmentId: {ShipmentId}",
            supplierId, shipmentId);

        try
        {
            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Supplier#{supplierId}" } },
                    { ":sk", new AttributeValue { S = $"Shipment#{shipmentId}" } }
                }
            );

            if (items == null || items.Count == 0)
            {
                _logger.LogWarning("Shipment not found - SupplierId: {SupplierId}, ShipmentId: {ShipmentId}",
                    supplierId, shipmentId);
                return null;
            }

            return SupplierShipmentMapper.ToDomain(items.First());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting shipment by ID - SupplierId: {SupplierId}, ShipmentId: {ShipmentId}",
                supplierId, shipmentId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza o status de um romaneio
    /// </summary>
    public async Task<bool> UpdateShipmentStatusAsync(string supplierId, string shipmentId, string status, string? shippedAt = null)
    {
        _logger.LogInformation(
            "Updating shipment status - SupplierId: {SupplierId}, ShipmentId: {ShipmentId}, Status: {Status}",
            supplierId, shipmentId, status);

        try
        {
            var shipment = await GetShipmentByIdAsync(supplierId, shipmentId);
            if (shipment == null)
            {
                _logger.LogWarning("Shipment not found for update - SupplierId: {SupplierId}, ShipmentId: {ShipmentId}",
                    supplierId, shipmentId);
                return false;
            }

            var updateExpression = "SET #status = :status";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":status", new AttributeValue { S = status } }
            };

            // Adicionar shipped_at se fornecido
            if (!string.IsNullOrWhiteSpace(shippedAt))
            {
                updateExpression += ", shipped_at = :shippedAt";
                expressionAttributeValues[":shippedAt"] = new AttributeValue { S = shippedAt };
            }

            await _repository.UpdateItemAsync(
                key: new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
                    { "SK", new AttributeValue { S = shipment.Sk } }
                },
                updateExpression: updateExpression,
                expressionAttributeValues: expressionAttributeValues
            );

            _logger.LogInformation(
                "Shipment status updated successfully - SupplierId: {SupplierId}, ShipmentId: {ShipmentId}, Status: {Status}",
                supplierId, shipmentId, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating shipment status - SupplierId: {SupplierId}, ShipmentId: {ShipmentId}",
                supplierId, shipmentId);
            throw;
        }
    }
}



