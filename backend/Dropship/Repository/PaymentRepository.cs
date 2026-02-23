using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com PaymentQueue no DynamoDB
/// Gerencia registros de pagamentos pendentes de fornecedores
/// </summary>
public class PaymentRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(DynamoDbRepository repository, ILogger<PaymentRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtém todos os pagamentos pendentes de um vendedor
    /// Busca registros com PK = "Seller#{sellerId}"
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentQueueBySellerId(string sellerId)
    {
        _logger.LogInformation("Getting payment queue by seller - SellerId: {SellerId}", sellerId);

        try
        {
            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Seller#{sellerId}" } },
                    { ":sk", new AttributeValue { S = "Payment#" } }
                }
            );

            var paymentQueue = items.Select(PaymentQueueMapper.ToDomain).ToList();
            
            _logger.LogInformation("Found {Count} payment queue entries for seller - SellerId: {SellerId}", 
                paymentQueue.Count, sellerId);
            
            return paymentQueue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment queue by seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém pagamentos pendentes de um vendedor filtrados por status
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentQueueBySellerAndStatus(string sellerId, string status)
    {
        _logger.LogInformation("Getting payment queue by seller and status - SellerId: {SellerId}, Status: {Status}", 
            sellerId, status);

        try
        {
            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Seller#{sellerId}" } },
                    { ":sk", new AttributeValue { S = "Payment#" } },
                    { ":status", new AttributeValue { S = status } }
                },
                filterExpression: "#status = :status"
            );

            var paymentQueue = items.Select(PaymentQueueMapper.ToDomain).ToList();
            
            _logger.LogInformation("Found {Count} payment queue entries - SellerId: {SellerId}, Status: {Status}", 
                paymentQueue.Count, sellerId, status);
            
            return paymentQueue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment queue by seller and status - SellerId: {SellerId}, Status: {Status}", 
                sellerId, status);
            throw;
        }
    }

    /// <summary>
    /// Obtém pagamentos de um fornecedor específico
    /// Busca registros onde SK começa com "PaymentQueue#Supplier#{supplierId}"
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentQueueBySupplier(string sellerId, string supplierId)
    {
        _logger.LogInformation("Getting payment queue by supplier - SellerId: {SellerId}, SupplierId: {SupplierId}", 
            sellerId, supplierId);

        try
        {
            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND begins_with(SK, :sk)",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Seller#{sellerId}" } },
                    { ":sk", new AttributeValue { S = $"Payments#" } },
                    { ":supplierId", new AttributeValue { S = supplierId } }
                },
                filterExpression: "supplier_id = :supplierId"
            );

            var paymentQueue = items.Select(PaymentQueueMapper.ToDomain).ToList();
            
            _logger.LogInformation("Found {Count} payment queue entries for supplier - SupplierId: {SupplierId}", 
                paymentQueue.Count, supplierId);
            
            return paymentQueue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment queue by supplier - SupplierId: {SupplierId}", supplierId);
            throw;
        }
    }
    /// <summary>
    /// Cria um novo registro de fila de pagamento
    /// </summary>
    public async Task CreatePaymentQueueAsync(PaymentQueueDomain paymentQueue)
    {
        _logger.LogInformation(
            "Creating payment queue - SellerId: {SellerId}, SupplierId: {SupplierId}, OrderSn: {OrderSn}",
            paymentQueue.SellerId, paymentQueue.SupplierId, paymentQueue.OrderSn);

        try
        {
            var item = paymentQueue.ToDynamoDb();

            await _repository.PutItemAsync(item);

            _logger.LogInformation("Payment queue created successfully - PaymentId: {PaymentId}, OrderSn: {OrderSn}",
                paymentQueue.PaymentId, paymentQueue.OrderSn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating payment queue - SellerId: {SellerId}, OrderSn: {OrderSn}",
                paymentQueue.SellerId, paymentQueue.OrderSn);
            throw;
        }
    }

    /// <summary>
    /// Obtém múltiplos pagamentos pelos seus IDs
    /// Usado para processar links InfinityPay com múltiplos pagamentos
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentsByIdsAsync(List<string> paymentIds)
    {
        _logger.LogInformation("Getting payments by IDs - Count: {Count}", paymentIds.Count);

        if (paymentIds == null || paymentIds.Count == 0)
        {
            _logger.LogWarning("No payment IDs provided");
            return new List<PaymentQueueDomain>();
        }

        try
        {
            var payments = new List<PaymentQueueDomain>();

            // Buscar cada pagamento pelo ID
            foreach (var paymentId in paymentIds)
            {
                var payment = await GetPaymentByIdAsync(paymentId);
                if (payment != null)
                {
                    payments.Add(payment);
                }
                else
                {
                    _logger.LogWarning("Payment not found - PaymentId: {PaymentId}", paymentId);
                }
            }

            _logger.LogInformation("Found {Count} payments out of {Total} requested",
                payments.Count, paymentIds.Count);

            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments by IDs");
            throw;
        }
    }

    /// <summary>
    /// Obtém um pagamento específico pelo seu PaymentId
    /// </summary>
    public async Task<PaymentQueueDomain?> GetPaymentByIdAsync(string paymentId)
    {
        _logger.LogInformation("Getting payment by ID - PaymentId: {PaymentId}", paymentId);

        try
        {
            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "SK = :sk",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":sk", new AttributeValue { S = $"Payment#{paymentId}" } }
                },
                indexName: "GSI_RELATIONS"
            );

            if (items == null || items.Count == 0)
            {
                _logger.LogWarning("Payment not found - PaymentId: {PaymentId}", paymentId);
                return null;
            }

            var payment = await _repository.GetItemAsync(items[0]);
            
            return PaymentQueueMapper.ToDomain(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment by ID - PaymentId: {PaymentId}", paymentId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza o status de um pagamento
    /// </summary>
    public async Task<bool> UpdatePaymentStatusAsync(string sellerId, string paymentId, string status, string? completedAt = null)
    {
        _logger.LogInformation("Updating payment status - SellerId: {SellerId}, PaymentId: {PaymentId}, Status: {Status}",
            sellerId, paymentId, status);

        try
        {
            // Buscar o pagamento primeiro para obter a SK completa
            var payment = await GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for update - PaymentId: {PaymentId}", paymentId);
                return false;
            }

            var updateExpression = "SET #status = :status";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":status", new AttributeValue { S = status } }
            };

            // Adicionar completed_at se fornecido
            if (!string.IsNullOrWhiteSpace(completedAt))
            {
                updateExpression += ", completed_at = :completedAt";
                expressionAttributeValues[":completedAt"] = new AttributeValue { S = completedAt };
            }

            await _repository.UpdateItemAsync(
                key: new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = $"Seller#{sellerId}" } },
                    { "SK", new AttributeValue { S = payment.SK } }
                },
                updateExpression: updateExpression,
                expressionAttributeValues: expressionAttributeValues
            );

            _logger.LogInformation("Payment status updated successfully - PaymentId: {PaymentId}, Status: {Status}",
                paymentId, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating payment status - SellerId: {SellerId}, PaymentId: {PaymentId}, Status: {Status}",
                sellerId, paymentId, status);
            throw;
        }
    }

}

