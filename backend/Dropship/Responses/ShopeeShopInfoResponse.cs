using System.Text.Json.Serialization;

namespace Dropship.Responses;

/// <summary>
/// Response das informações da loja Shopee
/// Baseado no endpoint v2.shop.get_shop_info
/// </summary>
public class ShopeeShopInfoResponse
{
    /// <summary>
    /// Nome da loja
    /// </summary>
    [JsonPropertyName("shop_name")]
    public string ShopName { get; set; } = string.Empty;

    /// <summary>
    /// Região/País da loja (sigla: BR, SG, etc)
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Status da loja (NORMAL, BANNED, etc)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Se é uma loja SIP (Service Inside Partner)
    /// </summary>
    [JsonPropertyName("is_sip")]
    public bool IsSip { get; set; }

    /// <summary>
    /// Se é uma loja CB (Cross Border)
    /// </summary>
    [JsonPropertyName("is_cb")]
    public bool IsCb { get; set; }

    /// <summary>
    /// ID único da requisição para rastreamento
    /// </summary>
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp Unix de quando o token foi autenticado
    /// </summary>
    [JsonPropertyName("auth_time")]
    public long AuthTime { get; set; }

    /// <summary>
    /// Timestamp Unix de quando o token expira
    /// </summary>
    [JsonPropertyName("expire_time")]
    public long ExpireTime { get; set; }

    /// <summary>
    /// Mensagem de erro (vazio se sucesso)
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem descritiva (sucesso ou detalhes do erro)
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Se está usando CBSC (Cross Border Seller Center) atualizado
    /// </summary>
    [JsonPropertyName("is_upgraded_cbsc")]
    public bool IsUpgradedCbsc { get; set; }

    /// <summary>
    /// ID do merchant (pode ser null)
    /// </summary>
    [JsonPropertyName("merchant_id")]
    public long? MerchantId { get; set; }

    /// <summary>
    /// Tipo de fulfillment (Others, FBA, etc)
    /// </summary>
    [JsonPropertyName("shop_fulfillment_flag")]
    public string ShopFulfillmentFlag { get; set; } = string.Empty;

    /// <summary>
    /// Se é a loja principal (em caso de múltiplas lojas)
    /// </summary>
    [JsonPropertyName("is_main_shop")]
    public bool IsMainShop { get; set; }

    /// <summary>
    /// Se é uma loja direct (venda direta)
    /// </summary>
    [JsonPropertyName("is_direct_shop")]
    public bool IsDirectShop { get; set; }

    /// <summary>
    /// ID da loja principal vinculada (0 se não há)
    /// </summary>
    [JsonPropertyName("linked_main_shop_id")]
    public long LinkedMainShopId { get; set; }

    /// <summary>
    /// Lista de IDs das lojas direct vinculadas
    /// </summary>
    [JsonPropertyName("linked_direct_shop_list")]
    public List<long> LinkedDirectShopList { get; set; } = new();

    /// <summary>
    /// Se utiliza um único AWB (Airway Bill) para múltiplos pedidos
    /// </summary>
    [JsonPropertyName("is_one_awb")]
    public bool IsOneAwb { get; set; }

    /// <summary>
    /// Se é uma loja mart (marketplace integrado)
    /// </summary>
    [JsonPropertyName("is_mart_shop")]
    public bool IsMartShop { get; set; }

    /// <summary>
    /// Se é uma loja outlet (venda de excedentes/defauts)
    /// </summary>
    [JsonPropertyName("is_outlet_shop")]
    public bool IsOutletShop { get; set; }
}
