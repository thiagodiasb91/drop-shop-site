using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Dropship.Repository;
using Dropship.Responses;

namespace Dropship.Services;

/// <summary>
/// Serviço para operações com pagamentos
/// Popula informações adicionais como supplierName
/// </summary>
public class PaymentService(
    PaymentRepository paymentRepository,
    SupplierShipmentRepository shipmentRepository,
    SupplierRepository supplierRepository,
    InfinityPayLinkRepository linkRepository,
    ILogger<PaymentService> logger,
    InfinityPayService infinityPayService)
{
    
    /// <summary>
    /// Obtém pagamentos do vendedor com informações do fornecedor
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentsBySellerId(string sellerId)
    {
        logger.LogInformation("Getting payments by seller - SellerId: {SellerId}", sellerId);

        try
        {
            var payments = await paymentRepository.GetPaymentQueueBySellerId(sellerId);

            if (payments == null || payments.Count == 0)
            {
                logger.LogInformation("No payments found for seller - SellerId: {SellerId}", sellerId);
                return [];
            }

            await EnrichPaymentsWithSupplierInfo(payments);

            logger.LogInformation("Returning {Count} payments - SellerId: {SellerId}", payments.Count, sellerId);

            return payments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payments by seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém pagamentos do vendedor filtrados por status com informações do fornecedor
    /// </summary>
    public async Task<List<PaymentQueueDomain>> GetPaymentsBySellerAndStatus(string sellerId, string status)
    {
        logger.LogInformation("Getting payments by seller and status - SellerId: {SellerId}, Status: {Status}", sellerId, status);

        try
        {
            var payments = await paymentRepository.GetPaymentQueueBySellerAndStatus(sellerId, status);

            if (payments == null || payments.Count == 0)
            {
                logger.LogInformation("No payments found - SellerId: {SellerId}, Status: {Status}", sellerId, status);
                return new List<PaymentQueueDomain>();
            }

            // Enriquecer com informações do fornecedor
            await EnrichPaymentsWithSupplierInfo(payments);

            logger.LogInformation("Returning {Count} payments - SellerId: {SellerId}, Status: {Status}", 
                payments.Count, sellerId, status);

            return payments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payments by seller and status - SellerId: {SellerId}, Status: {Status}", 
                sellerId, status);
            throw;
        }
    }

    /// <summary>
    /// Cria um novo registro de fila de pagamento para um fornecedor
    /// </summary>
    public async Task CreatePaymentQueueAsync(PaymentQueueDomain paymentQueue)
    {
        logger.LogInformation(
            "Creating payment queue - SellerId: {SellerId}, SupplierId: {SupplierId}, OrderSn: {OrderSn}",
            paymentQueue.SellerId, paymentQueue.SupplierId, paymentQueue.OrderSn);

        try
        {
            await paymentRepository.CreatePaymentQueueAsync(paymentQueue);

            logger.LogInformation(
                "Payment queue created - SellerId: {SellerId}, SupplierId: {SupplierId}, OrderSn: {OrderSn}",
                paymentQueue.SellerId, paymentQueue.SupplierId, paymentQueue.OrderSn);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
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
        string linkId,
        decimal paidAmount,
        int installments,
        string transactionNsu,
        string captureMethod,
        string receiptUrl)
    {
        logger.LogInformation(
            "Processing payment webhook with link - WebhookOrderNsu: {OrderNsu}, Amount: {Amount}",
            linkId, paidAmount);

        try
        {
            // 1. Buscar o link pelo webhookOrderNsu
            var link = await linkRepository.GetLinkByIdAsync(linkId);
            
            if (link == null)
            {
                throw new InvalidOperationException(
                    $"InfinityPay link not found - WebhookOrderNsu: {linkId}");
            }

            logger.LogInformation(
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
                        captureMethod: captureMethod,
                        receiptUrl: receiptUrl);

                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    logger.LogError(ex,
                        "Error processing payment from webhook - PaymentId: {PaymentId}, LinkId: {LinkId}",
                        paymentId, link.LinkId);
                }
            }

            // 3. Atualizar status do link para "completed"
            await linkRepository.UpdateLinkStatusAsync(
                linkId: link.LinkId,
                status: "completed",
                completedAt: DateTime.UtcNow.ToString("O"));

            logger.LogInformation(
                "InfinityPay webhook processed with link - LinkId: {LinkId}, SuccessCount: {Success}, FailureCount: {Failure}",
                link.LinkId, successCount, failureCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error processing payment webhook with link - WebhookOrderNsu: {OrderNsu}",
                linkId);
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
        string captureMethod,
        string receiptUrl)
    {
        logger.LogInformation(
            "Processing payment from webhook - PaymentId: {PaymentId}, Amount: {Amount}",
            paymentId, paidAmount);

        try
        {
            // 1. Buscar o pagamento pendente pelo paymentId
            var payment = await paymentRepository.GetPaymentByIdAsync(paymentId);
            
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

            payment.Status = "paid";
            payment.CompletedAt = DateTime.UtcNow.ToString("O");
            // 3. Atualizar status do pagamento para "paid"
            await paymentRepository.UpdatePayment(payment);

            logger.LogInformation(
                "Payment status updated to 'paid' - PaymentId: {PaymentId}, SellerId: {SellerId}", 
                paymentId, payment.SellerId);

            // 4. Criar registro de romaneio (shipment) do fornecedor
            var shipment = SupplierShipmentBuilder.CreateFromPayment(
                paymentQueue: payment,
                transactionNsu: transactionNsu,
                captureMethod: captureMethod,
                receiptUrl: receiptUrl,
                paidAmount: paidAmount,
                installments: installments);

            await shipmentRepository.CreateShipmentAsync(shipment);

            logger.LogInformation(
                "Supplier shipment created from payment - ShipmentId: {ShipmentId}, SupplierId: {SupplierId}, PaymentId: {PaymentId}",
                shipment.ShipmentId, shipment.SupplierId, paymentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
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
                var supplier = await supplierRepository.GetSupplierAsync(supplierId);
                var supplierName = supplier?.Name ?? "Unknown Supplier";

                // Atualizar todos os pagamentos deste fornecedor com o nome
                foreach (var payment in payments.Where(p => p.SupplierId == supplierId))
                {
                    payment.SupplierName = supplierName;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error getting supplier info - SupplierId: {SupplierId}", supplierId);
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
        logger.LogInformation(
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
            var payments = await paymentRepository.GetPaymentsByIdsAsync(paymentIds);
            
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

            //valida que todos os pending sõa do mesmo fornecedor
            var sameSupplier = payments.Select(x => x.SupplierId).Distinct();
            
            if (sameSupplier.Count() > 1)
            {
                throw new InvalidOperationException(
                    $"All payments must be from the same supplier. Found suppliers: {string.Join(", ", sameSupplier)}");
            }
            
            var supplier = await supplierRepository.GetSupplierAsync(sameSupplier.First());
            var items = payments.SelectMany( x=> x.PaymentProducts)
                .Select(p => new InfinityPayItem
                {
                    Description = p.Name,
                    Quantity = p.Quantity,
                    Price = Convert.ToInt64(p.UnitPrice)
                }).ToList();
            var linkId = Ulid.NewUlid().ToString();
            
            var infinityPayUrl = await infinityPayService.CreateLinkAsync(supplier.InfinityPayHandle,
                items, 
                linkId,
                Environment.GetEnvironmentVariable("WEBHOOK_INFINITYPAY_URL")
                );
            
            // Criar o link
            var infinityPay = InfinityPayLinkBuilder.Create(sellerId, linkId, paymentIds, totalAmount, infinityPayUrl);
            await linkRepository.CreateLinkAsync(infinityPay);

            foreach (var payment in payments)
            {
                payment.InfinityPayUrl = infinityPayUrl;
                payment.Status = "waiting-payment";
                await paymentRepository.UpdatePayment(payment);   
            }
            
            logger.LogInformation(
                "InfinityPay link created - LinkId: {LinkId}, PaymentIds: {Count}",
                infinityPay.LinkId, paymentIds.Count);

            return infinityPay;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error creating InfinityPay link - SellerId: {SellerId}, PaymentCount: {Count}",
                sellerId, paymentIds.Count);
            throw;
        }
    }
}

