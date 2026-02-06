namespace Dropship.Responses;

public class PaymentOrderResponse
{
    public string Sku { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PaymentSupplierResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TotalDue { get; set; }
    public int ProductsCount { get; set; }
    public List<PaymentOrderResponse> Orders { get; set; } = new();
}