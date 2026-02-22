using System.Text.Json.Serialization;

namespace Dropship.Requests;

/// <summary>
/// Request para processar pedido
/// </summary>
public class ProcessOrderRequest
{
    /// <summary>
    /// Número da ordem (Order SN)
    /// </summary>
    [JsonPropertyName("ordersn")]
    public string OrderSn { get; set; } = string.Empty;

    /// <summary>
    /// Status da ordem
    /// Valores possíveis: UNPAID, PAID, SHIPPED, DELIVERED, CANCELLED, etc.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp Unix da última atualização (em segundos)
    /// </summary>
    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }

    /// <summary>
    /// ID da loja Shopee (shop_id)
    /// </summary>
    [JsonPropertyName("shop_id")]
    public long ShopId { get; set; }
}

