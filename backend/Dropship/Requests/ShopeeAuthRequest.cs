using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dropship.Requests;

/// <summary>
/// Request para autenticação com Shopee
/// Contém code, shop_id e email para autenticação OAuth2
/// </summary>
public class ShopeeAuthRequest
{
    /// <summary>
    /// Authorization code recebido do Shopee via callback
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    [StringLength(500, ErrorMessage = "Code must not exceed 500 characters")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// ID da loja Shopee
    /// </summary>
    [Required(ErrorMessage = "ShopId is required")]
    [Range(1, long.MaxValue, ErrorMessage = "ShopId must be a positive number")]
    public long ShopId { get; set; }

    /// <summary>
    /// Email do usuário (seller)
    /// Deve existir na tabela de usuários
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    public string Email { get; set; } = string.Empty;
}
