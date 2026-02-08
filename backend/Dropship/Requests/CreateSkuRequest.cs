namespace Dropship.Requests;

/// <summary>
/// Request para criar um novo SKU
/// </summary>
public class CreateSkuRequest
{
    /// <summary>
    /// ID do produto ao qual o SKU pertence
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Código SKU único
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Tamanho do produto
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Cor do produto
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Request para atualizar um SKU existente
/// </summary>
public class UpdateSkuRequest
{
    /// <summary>
    /// Tamanho do produto (opcional)
    /// </summary>
    public string? Size { get; set; }

    /// <summary>
    /// Cor do produto (opcional)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Quantidade em estoque (opcional)
    /// </summary>
    public int? Quantity { get; set; }
}
