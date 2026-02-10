using System.ComponentModel.DataAnnotations;

namespace Dropship.Requests;

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