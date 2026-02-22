using Microsoft.AspNetCore.Mvc;
using Dropship.Requests;
using Dropship.Services;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciamento de pedidos
/// Integra processamento de pedidos da Shopee com atualização de estoque
/// </summary>
[ApiController]
[Route("orders")]
public class OrdersController(OrderProcessingService orderProcessingService, ILogger<OrdersController> logger)
    : ControllerBase
{
    /// <summary>
    /// Processa um pedido realizado na Shopee
    /// 
    /// Fluxo de processamento:
    /// 1. Valida se o status é "READY_TO_SHIP"
    /// 2. Obtém detalhes do pedido via API Shopee
    /// 3. Para cada SKU no pedido:
    ///    - Busca fornecedores disponíveis (ordenado por prioridade e quantidade)
    ///    - Atualiza estoque do fornecedor (subtrai quantidade)
    ///    - Registra movimentação no Kardex
    ///    - Cria registro na fila de pagamento do fornecedor
    /// </summary>
    /// <param name="request">Dados do pedido a processar</param>
    /// <returns>Resultado do processamento</returns>
    [HttpPost("process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessOrder([FromBody] ProcessOrderRequest request)
    {
        logger.LogInformation("[ORDERS] Processing order - OrderSn: {OrderSn}, Status: {Status}, ShopId: {ShopId}",
            request?.OrderSn, request?.Status, request?.ShopId);

        if (request is null)
        {
            logger.LogWarning("[ORDERS] Request is null");
            return BadRequest(new { error = "Requisição inválida" });
        }

        if (string.IsNullOrWhiteSpace(request.OrderSn) || string.IsNullOrWhiteSpace(request.Status))
        {
            logger.LogWarning("[ORDERS] Missing required fields - OrderSn: {OrderSn}, Status: {Status}",
                request.OrderSn, request.Status);
            return BadRequest(new { error = "OrderSn e Status são obrigatórios" });
        }

        if (request.ShopId <= 0)
        {
            logger.LogWarning("[ORDERS] Invalid ShopId: {ShopId}", request.ShopId);
            return BadRequest(new { error = "ShopId válido é obrigatório" });
        }

        try
        {
            // Processar o pedido
            var result = await orderProcessingService.ProcessOrderAsync(
                request.OrderSn,
                request.Status,
                request.ShopId);

            if (!result)
            {
                logger.LogWarning("[ORDERS] Order not processed - OrderSn: {OrderSn}, Status: {Status} (status not READY_TO_SHIP)",
                    request.OrderSn, request.Status);
                
                return Ok(new
                {
                    message = "Pedido não foi processado",
                    details = "Status deve ser 'READY_TO_SHIP'",
                    orderSn = request.OrderSn,
                    status = request.Status,
                    shopId = request.ShopId
                });
            }

            logger.LogInformation("[ORDERS] Order processed successfully - OrderSn: {OrderSn}", request.OrderSn);

            return Ok(new
            {
                message = "Pedido processado com sucesso",
                orderSn = request.OrderSn,
                status = request.Status,
                shopId = request.ShopId,
                updateTime = request.UpdateTime
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[ORDERS] Error processing order - OrderSn: {OrderSn}, ShopId: {ShopId}",
                request.OrderSn, request.ShopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}

