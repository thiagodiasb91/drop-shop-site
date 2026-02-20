using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dropship.Requests;

/// <summary>
/// Request para inicializar variações de tier em um item existente
/// Ref: https://open.shopee.com/documents/v2/v2.product.init_tier_variation
/// </summary>
public class InitTierVariationRequest
{
    /// <summary>
    /// Lista de variações padronizadas (ex: Color, Size)
    /// </summary>
    [Required]
    [JsonPropertyName("standardise_tier_variation")]
    public List<StandardiseTierVariation> StandardiseTierVariation { get; set; } = new();

    /// <summary>
    /// Lista de modelos para cada combinação de variações
    /// </summary>
    [Required]
    [JsonPropertyName("model")]
    public List<ModelItem> Model { get; set; } = new();
}

/// <summary>
/// Variação padronizada (ex: Color, Size)
/// </summary>
public class StandardiseTierVariation
{
    /// <summary>
    /// ID da variação (usar 0 para nova variação)
    /// </summary>
    /// <example>0</example>
    [JsonPropertyName("variation_id")]
    public long VariationId { get; set; } = 0;

    /// <summary>
    /// ID do grupo de variação (usar 0 para novo grupo)
    /// </summary>
    /// <example>0</example>
    [JsonPropertyName("variation_group_id")]
    public long VariationGroupId { get; set; } = 0;

    /// <summary>
    /// Nome da variação (ex: Color, Size)
    /// </summary>
    /// <example>Color</example>
    [Required]
    [JsonPropertyName("variation_name")]
    public string VariationName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de opções da variação
    /// </summary>
    [Required]
    [JsonPropertyName("variation_option_list")]
    public List<VariationOptionItem> VariationOptionList { get; set; } = new();
}

/// <summary>
/// Opção de variação (ex: Azul, P, M, G)
/// </summary>
public class VariationOptionItem
{
    /// <summary>
    /// ID da opção de variação (usar 0 para nova opção)
    /// </summary>
    /// <example>0</example>
    [JsonPropertyName("variation_option_id")]
    public long VariationOptionId { get; set; } = 0;

    /// <summary>
    /// Nome da opção de variação
    /// </summary>
    /// <example>Azul</example>
    [Required]
    [JsonPropertyName("variation_option_name")]
    public string VariationOptionName { get; set; } = string.Empty;

    /// <summary>
    /// ID da imagem da opção (opcional, obter via upload de imagem)
    /// </summary>
    /// <example>sg-11134201-7r98o-xxx</example>
    [JsonPropertyName("image_id")]
    public string? ImageId { get; set; }
}

/// <summary>
/// Item de modelo/SKU para cada combinação de variações
/// </summary>
public class ModelItem
{
    /// <summary>
    /// Índices das opções selecionadas em cada tier_variation
    /// Ex: [0, 0] = primeira opção da primeira variação + primeira opção da segunda variação
    /// </summary>
    /// <example>[0, 0]</example>
    [Required]
    [JsonPropertyName("tier_index")]
    public List<int> TierIndex { get; set; } = new();

    /// <summary>
    /// SKU do modelo
    /// </summary>
    /// <example>CAM-AZUL-P</example>
    [JsonPropertyName("model_sku")]
    public string? ModelSku { get; set; }

    /// <summary>
    /// Preço original para este modelo (em centavos para algumas regiões)
    /// </summary>
    /// <example>10000</example>
    [Required]
    [JsonPropertyName("original_price")]
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Estoque do seller por localização
    /// </summary>
    [JsonPropertyName("seller_stock")]
    public List<SellerStockItem>? SellerStock { get; set; }

    /// <summary>
    /// Quantidade em estoque para este modelo (alternativa ao seller_stock)
    /// </summary>
    /// <example>10</example>
    [JsonPropertyName("normal_stock")]
    public int? NormalStock { get; set; }
}

/// <summary>
/// Estoque do seller por localização
/// </summary>
public class SellerStockItem
{
    /// <summary>
    /// ID da localização (opcional)
    /// </summary>
    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }

    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    /// <example>10</example>
    [Required]
    [JsonPropertyName("stock")]
    public int Stock { get; set; }
}

