using Dropship.Domain;
using Dropship.Repository;

namespace Dropship.Services;

/// <summary>
/// Serviço para operações com pagamentos
/// Popula informações adicionais como supplierName
/// </summary>
public class PaymentService
{
    private readonly PaymentRepository _paymentRepository;
    private readonly SupplierShipmentRepository _shipmentRepository;
    private readonly SupplierRepository _supplierRepository;
    private readonly InfinityPayLinkRepository _linkRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PaymentRepository paymentRepository,
        SupplierShipmentRepository shipmentRepository,
        SupplierRepository supplierRepository,
        InfinityPayLinkRepository linkRepository,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _shipmentRepository = shipmentRepository;
        _supplierRepository = supplierRepository;
        _linkRepository = linkRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtém pagamentos do vendedor com informações do fornecedor
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentsBySellerId(string sellerId)
    {
        _logger.LogInformation("Getting payments by seller - SellerId: {SellerId}", sellerId);

        try
        {
            var payments = await _paymentRepository.GetPaymentQueueBySellerId(sellerId);

            if (payments == null || payments.Count == 0)
            {
                _logger.LogInformation("No payments found for seller - SellerId: {SellerId}", sellerId);
                return [];
            }

            await EnrichPaymentsWithSupplierInfo(payments);

            _logger.LogInformation("Returning {Count} payments - SellerId: {SellerId}", payments.Count, sellerId);

            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments by seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém pagamentos do vendedor filtrados por status com informações do fornecedor
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentsBySellerAndStatus(string sellerId, string status)
    {
        _logger.LogInformation("Getting payments by seller and status - SellerId: {SellerId}, Status: {Status}", sellerId, status);

        try
        {
            var payments = await _paymentRepository.GetPaymentQueueBySellerAndStatus(sellerId, status);

            if (payments == null || payments.Count == 0)
            {
                _logger.LogInformation("No payments found - SellerId: {SellerId}, Status: {Status}", sellerId, status);
                return new List<PaymentQueueDomain>();
            }

            // Enriquecer com informações do fornecedor
            await EnrichPaymentsWithSupplierInfo(payments);

            _logger.LogInformation("Returning {Count} payments - SellerId: {SellerId}, Status: {Status}", 
                payments.Count, sellerId, status);

            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments by seller and status - SellerId: {SellerId}, Status: {Status}", 
                sellerId, status);
            throw;
        }
    }

    /// <summary>
    /// Cria um novo registro de fila de pagamento para um fornecedor
    /// </summary>
    public async Task CreatePaymentQueueAsync(PaymentQueueDomain paymentQueue)
    {
        _logger.LogInformation(
            "Creating payment queue - SellerId: {SellerId}, SupplierId: {SupplierId}, OrderSn: {OrderSn}",
            paymentQueue.SellerId, paymentQueue.SupplierId, paymentQueue.OrderSn);

        try
        {
            await _paymentRepository.CreatePaymentQueueAsync(paymentQueue);

            _logger.LogInformation(
                "Payment queue created - SellerId: {SellerId}, SupplierId: {SupplierId}, OrderSn: {OrderSn}",
                paymentQueue.SellerId, paymentQueue.SupplierId, paymentQueue.OrderSn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating payment queue - SellerId: {SellerId}, SupplierId: {SupplierId}, OrderSn: {OrderSn}",
                paymentQueue.SellerId, paymentQueue.SupplierId, paymentQueue.OrderSn);
            throw;
        }
    }

    /// <summary>
    /// Processa um pagamento recebido do webhook InfinityPay
    /// Busca o link pelo webhookOrderNsu e marca como completo
    /// </summary>
    public async Task ProcessPaymentWebhookWithLinkAsync(
        string webhookOrderNsu,
        decimal paidAmount,
        int installments,
        string transactionNsu,
        string orderNsu,
        string captureMethod,
        string receiptUrl)
    {
        _logger.LogInformation(
            "Processing payment webhook with link - WebhookOrderNsu: {OrderNsu}, Amount: {Amount}",
            webhookOrderNsu, paidAmount);

        try
        {
            // 1. Buscar o link pelo webhookOrderNsu
            var link = await _linkRepository.GetLinkByWebhookOrderNsuAsync(webhookOrderNsu);
            
            if (link == null)
            {
                throw new InvalidOperationException(
                    $"InfinityPay link not found - WebhookOrderNsu: {webhookOrderNsu}");
            }

            _logger.LogInformation(
                "Found InfinityPay link - LinkId: {LinkId}, PaymentCount: {Count}",
                link.LinkId, link.PaymentCount);

            // 2. Processar cada pagamento do link
            var successCount = 0;
            var failureCount = 0;

            foreach (var paymentId in link.PaymentIds)
            {
                try
                {
                    await ProcessPaymentFromWebhookAsync(
                        paymentId: paymentId,
                        paidAmount: paidAmount,
                        installments: installments,
                        transactionNsu: transactionNsu,
                        orderNsu: orderNsu,
                        captureMethod: captureMethod,
                        receiptUrl: receiptUrl);

                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex,
                        "Error processing payment from webhook - PaymentId: {PaymentId}, LinkId: {LinkId}",
                        paymentId, link.LinkId);
                }
            }

            // 3. Atualizar status do link para "completed"
            await _linkRepository.UpdateLinkStatusAsync(
                linkId: link.LinkId,
                status: "completed",
                completedAt: DateTime.UtcNow.ToString("O"));

            _logger.LogInformation(
                "InfinityPay webhook processed with link - LinkId: {LinkId}, SuccessCount: {Success}, FailureCount: {Failure}",
                link.LinkId, successCount, failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing payment webhook with link - WebhookOrderNsu: {OrderNsu}",
                webhookOrderNsu);
            throw;
        }
    }

    /// <summary>
    /// Processa um pagamento recebido do webhook InfinityPay
    /// Busca o PaymentQueue pelo paymentId e atualiza o status
    /// </summary>
    public async Task ProcessPaymentFromWebhookAsync(
        string paymentId,
        decimal paidAmount,
        int installments,
        string transactionNsu,
        string orderNsu,
        string captureMethod,
        string receiptUrl)
    {
        _logger.LogInformation(
            "Processing payment from webhook - PaymentId: {PaymentId}, Amount: {Amount}",
            paymentId, paidAmount);

        try
        {
            // 1. Buscar o pagamento pendente pelo paymentId
            var payment = await _paymentRepository.GetPaymentByIdAsync(paymentId);
            
            if (payment == null)
            {
                throw new InvalidOperationException(
                    $"Payment not found - PaymentId: {paymentId}");
            }

            // 2. Validar que o status está "pending"
            if (payment.Status != "pending")
            {
                throw new InvalidOperationException(
                    $"Payment already processed - PaymentId: {paymentId}, Current Status: {payment.Status}");
            }

            // 3. Atualizar status do pagamento para "paid"
            await _paymentRepository.UpdatePaymentStatusAsync(
                sellerId: payment.SellerId,
                paymentId: paymentId,
                status: "paid",
                completedAt: DateTime.UtcNow.ToString("O"));

            _logger.LogInformation(
                "Payment status updated to 'paid' - PaymentId: {PaymentId}, SellerId: {SellerId}", 
                paymentId, payment.SellerId);

            // 4. Criar registro de romaneio (shipment) do fornecedor
            var shipment = SupplierShipmentBuilder.CreateFromPayment(
                paymentQueue: payment,
                transactionNsu: transactionNsu,
                orderNsu: orderNsu,
                captureMethod: captureMethod,
                receiptUrl: receiptUrl,
                paidAmount: paidAmount,
                installments: installments);

            await _shipmentRepository.CreateShipmentAsync(shipment);

            _logger.LogInformation(
                "Supplier shipment created from payment - ShipmentId: {ShipmentId}, SupplierId: {SupplierId}, PaymentId: {PaymentId}",
                shipment.ShipmentId, shipment.SupplierId, paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing payment from webhook - PaymentId: {PaymentId}",
                paymentId);
            throw;
        }
    }

    /// <summary>
    /// Enriquece a lista de pagamentos com informações do fornecedor (supplierName)
    /// </summary>
    private async Task EnrichPaymentsWithSupplierInfo(List<PaymentQueueDomain> payments)
    {
        var supplierIds = payments.Select(p => p.SupplierId).Distinct().ToList();

        foreach (var supplierId in supplierIds)
        {
            try
            {
                var supplier = await _supplierRepository.GetSupplierAsync(supplierId);
                var supplierName = supplier?.Name ?? "Unknown Supplier";

                // Atualizar todos os pagamentos deste fornecedor com o nome
                foreach (var payment in payments.Where(p => p.SupplierId == supplierId))
                {
                    payment.SupplierName = supplierName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting supplier info - SupplierId: {SupplierId}", supplierId);
            }
        }
    }

    /// <summary>
    /// Cria um link de pagamento para InfinityPay com múltiplos paymentIds
    /// Retorna um LinkId (ULID) que será usado na URL de checkout
    /// </summary>
    public async Task<InfinityPayLinkDomain> CreateInfinityPayLinkAsync(
        string sellerId,
        List<string> paymentIds,
        decimal totalAmount)
    {
        _logger.LogInformation(
            "Creating InfinityPay link - SellerId: {SellerId}, PaymentIds: {Count}, Amount: {Amount}",
            sellerId, paymentIds.Count, totalAmount);

        try
        {
            // Validar que há pagementIds
            if (paymentIds == null || paymentIds.Count == 0)
            {
                throw new InvalidOperationException("At least one payment ID is required");
            }

            // Validar que todos os pagamentos existem e estão pending
            var payments = await _paymentRepository.GetPaymentsByIdsAsync(paymentIds);
            
            if (payments.Count != paymentIds.Count)
            {
                var notFound = paymentIds.Except(payments.Select(p => p.PaymentId)).ToList();
                throw new InvalidOperationException(
                    $"Some payments were not found: {string.Join(", ", notFound)}");
            }

            // Validar que todos estão com status pending
            var nonPending = payments.Where(p => p.Status != "pending").ToList();
            if (nonPending.Count > 0)
            {
                throw new InvalidOperationException(
                    $"All payments must be pending. Found {nonPending.Count} with status: {string.Join(", ", nonPending.Select(p => p.Status).Distinct())}");
            }

            // Criar o link
            var link = InfinityPayLinkBuilder.Create(sellerId, paymentIds, totalAmount);
            
            _logger.LogInformation(
                "InfinityPay link created - LinkId: {LinkId}, PaymentIds: {Count}",
                link.LinkId, paymentIds.Count);

            return link;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating InfinityPay link - SellerId: {SellerId}, PaymentCount: {Count}",
                sellerId, paymentIds.Count);
            throw;
        }
    }
}

