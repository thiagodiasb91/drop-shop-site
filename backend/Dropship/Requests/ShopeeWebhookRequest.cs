using System.Text.Json.Serialization;

namespace Dropship.Requests;

/// <summary>
/// Request para webhook de Shopee
/// Estrutura: https://shopee.com/docs
/// </summary>
public class ShopeeWebhookRequest
{
    /// <summary>
    /// ID único da mensagem (para garantir idempotência)
    /// </summary>
    [JsonPropertyName("msg_id")]
    public string MsgId { get; set; } = string.Empty;

    /// <summary>
    /// ID da loja Shopee (shop_id)
    /// </summary>
    [JsonPropertyName("shop_id")]
    public long ShopId { get; set; }

    /// <summary>
    /// Código do evento (3 = nova ordem recebida)
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Timestamp Unix do evento (segundos)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Dados do evento
    /// </summary>
    [JsonPropertyName("data")]
    public ShopeeWebhookData? Data { get; set; }
}

/// <summary>
/// Dados do webhook de Shopee
/// </summary>
public class ShopeeWebhookData
{
    /// <summary>
    /// Número da ordem (ordersn)
    /// </summary>
    [JsonPropertyName("ordersn")]
    public string OrderSn { get; set; } = string.Empty;

    /// <summary>
    /// Status da ordem (UNPAID, PAID, SHIPPED, COMPLETED, etc.)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp Unix da atualização (segundos)
    /// </summary>
    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }

    /// <summary>
    /// Cenário completado (geralmente vazio)
    /// </summary>
    [JsonPropertyName("completed_scenario")]
    public string CompletedScenario { get; set; } = string.Empty;

    /// <summary>
    /// Itens da ordem (produtos)
    /// </summary>
    [JsonPropertyName("items")]
    public List<ShopeeOrderItem> Items { get; set; } = new();
}

/// <summary>
/// Item (produto) da ordem
/// </summary>
public class ShopeeOrderItem
{
    /// <summary>
    /// ID do item
    /// </summary>
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// ID variação (SKU)
    /// </summary>
    [JsonPropertyName("variation_id")]
    public string VariationId { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Preço
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}


