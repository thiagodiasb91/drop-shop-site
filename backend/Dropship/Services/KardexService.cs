using Dropship.Domain;
using Dropship.Repository;

namespace Dropship.Services;

/// <summary>
/// Serviço para operações com Kardex
/// Gerencia o registro de movimentação de estoque
/// </summary>
public class KardexService
{
    private readonly KardexRepository _kardexRepository;
    private readonly ILogger<KardexService> _logger;

    public KardexService(KardexRepository kardexRepository, ILogger<KardexService> logger)
    {
        _kardexRepository = kardexRepository;
        _logger = logger;
    }

    /// <summary>
    /// Adiciona um registro ao Kardex para rastreamento de movimentação de estoque
    /// </summary>
    public async Task AddToKardexAsync(
        string productId,
        string sku,
        int quantity,
        string orderSn,
        long shopId,
        string supplierId)
    {
        _logger.LogInformation(
            "Adding to Kardex - ProductId: {ProductId}, SKU: {SKU}, Quantity: {Quantity}, OrderSn: {OrderSn}",
            productId, sku, quantity, orderSn);

        try
        {
            var kardex = new KardexDomain
            {
                PK = $"Kardex#Sku#{sku}",
                SK = Ulid.NewUlid().ToString(),
                ProductId = productId,
                EntityType = "kardex",
                Quantity = quantity,
                Operation = "remove",
                Date = DateTime.UtcNow.ToString("O"),
                SupplierId = supplierId,
                OrderSn = orderSn,
                ShopId = shopId
            };

            await _kardexRepository.AddToKardexAsync(kardex);

            _logger.LogInformation("Added to Kardex - ProductId: {ProductId}, SKU: {SKU}",
                productId, sku);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to Kardex - ProductId: {ProductId}, SKU: {SKU}",
                productId, sku);
            throw;
        }
    }
}

