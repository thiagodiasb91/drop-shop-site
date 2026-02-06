namespace Dropship.Domain;

public class StockDomain
{
    public string SupplierId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ProductId { get; set; } = string.Empty;
}