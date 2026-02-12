using System.ComponentModel.DataAnnotations;

namespace Dropship.Requests;

/// <summary>
/// Request para vincular um vendedor a um produto
/// </summary>
public class LinkSellerToProductRequest
{
    /// <summary>
    /// ID da loja no marketplace (ex: shop_id para Shopee)
    /// </summary>
    /// <example>226477144</example>
    [Required]
    public long StoreId { get; set; }

    /// <summary>
    /// Preço do produto no marketplace
    /// </summary>
    /// <example>12.90</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    /// <summary>
    /// Mapeamento de SKUs com IDs do marketplace
    /// </summary>
    [Required]
    public List<SkuMapping> SkuMappings { get; set; } = new();
}

/// <summary>
/// Mapeamento de SKU com IDs do marketplace
/// </summary>
public class SkuMapping
{
    /// <summary>
    /// Código do SKU
    /// </summary>
    /// <example>CROSS_P</example>
    [Required]
    public string Sku { get; set; } = string.Empty;
    /// <summary>
    /// Preço do produto no marketplace
    /// </summary>
    /// <example>12.90</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
}

/// <summary>
/// Request para atualizar preço de um SKU vendido
/// </summary>
public class UpdateSellerPriceRequest
{
    /// <summary>
    /// Novo preço
    /// </summary>
    /// <example>15.90</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
}