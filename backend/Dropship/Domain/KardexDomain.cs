namespace Dropship.Domain;

public class KardexDomain
{
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public string? OrderSn { get; set; }
    public long? ShopId { get; set; }
}