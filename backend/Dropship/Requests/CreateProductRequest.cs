using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dropship.Requests;

/// <summary>
/// Request para criar um novo produto
/// </summary>
public class CreateProductRequest
{
    /// <summary>
    /// Nome do produto
    /// </summary>
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 500 characters")]
    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;
}

/// <summary>
/// Request para atualizar um produto existente
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// Nome do produto (opcional)
    /// </summary>
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 500 characters")]
    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }
}
