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
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(PaymentRepository paymentRepository, SupplierRepository supplierRepository, ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _supplierRepository = supplierRepository;
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
                return new List<PaymentQueueDomain>();
            }

            // Enriquecer com informações do fornecedor
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
}

