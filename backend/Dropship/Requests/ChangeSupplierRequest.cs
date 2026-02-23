namespace Dropship.Requests;

/// <summary>
/// Request para trocar o fornecedor de um produto do vendedor
/// </summary>
public class ChangeSupplierRequest
{
    /// <summary>
    /// ID do novo fornecedor
    /// </summary>
    public string NewSupplierId { get; set; } = string.Empty;
}

