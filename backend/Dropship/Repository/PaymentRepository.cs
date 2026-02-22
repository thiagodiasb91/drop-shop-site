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

}

