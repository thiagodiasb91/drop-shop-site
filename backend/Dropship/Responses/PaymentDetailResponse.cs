namespace Dropship.Responses;

/// <summary>
/// Response para sumário de pagamento do vendedor
/// Lista pagamentos agrupados por fornecedor
/// </summary>
public class PaymentSummaryItemResponse
{
    public string PaymentId { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    public string SupplierName { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public string Status { get; set; } = default!;  // pending, paid, failed
    public string? DueDate { get; set; }  // "YYYY-MM-DD"
    public string? PaidAt { get; set; }  // "YYYY-MM-DD"
}

public class PaymentSummaryListResponse
{
    public List<PaymentSummaryItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Response para detalhes de um pagamento
/// </summary>
public class PaymentDetailItemResponse
{
    public string Id { get; set; } = default!;  // SKU or Product ID
    public string Name { get; set; } = default!;  // Product/SKU name
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string OrderId { get; set; } = default!;
}

public class PaymentDetailResponse
{
    public string PaymentId { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    public string SupplierName { get; set; } = default!;
    public decimal Total { get; set; }
    public string Status { get; set; } = default!;
    public string CreatedAt { get; set; } = default!;
    public string? PaidAt { get; set; }
    public List<PaymentDetailItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Response para marcação de pagamento como pago
/// </summary>
public class MarkPaymentPaidResponse
{
    public string PaymentId { get; set; } = default!;
    public string Status { get; set; } = "paid";
    public string PaidAt { get; set; } = default!;
    public string Message { get; set; } = "Payment marked as paid successfully";
}

