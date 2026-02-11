using System.ComponentModel.DataAnnotations;

namespace Dropship.Requests;

/// <summary>
/// Request para vincular um fornecedor a um produto
/// </summary>
public class LinkSupplierToProductRequest
{
    /// <summary>
    /// Preço de produção do fornecedor
    /// </summary>
    /// <example>49.90</example>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Production price must be greater than 0")]
    public decimal ProductionPrice { get; set; }
    
    [Required]
    public List<SkuReference> Skus { get; set; } = new();
}

public class SkuReference
{
    [Required]
    public string Sku { get; set; } = string.Empty;
    
    [Required]
    public string SkuSupplier { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }
}

/// <summary>
/// Request para atualizar preço e quantidade de um SKU fornecido
/// </summary>
public class UpdateSupplierPricingRequest
{
    /// <summary>
    /// Novo preço de produção
    /// </summary>
    /// <example>45.50</example>
    [Range(0.01, double.MaxValue, ErrorMessage = "Production price must be greater than 0")]
    public decimal? ProductionPrice { get; set; }

    /// <summary>
    /// Quantidade disponível no fornecedor
    /// </summary>
    /// <example>100</example>
    [Range(0, long.MaxValue, ErrorMessage = "Quantity cannot be negative")]
    public long? Quantity { get; set; }
}

