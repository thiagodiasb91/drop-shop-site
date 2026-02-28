namespace Dropship.Responses;

/// <summary>
/// Response de um item dentro do romaneio
/// </summary>
public class ShipmentItemResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SkuId { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>
/// Response completo de um romaneio para exibição/impressão
/// </summary>
public class ShipmentResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<ShipmentItemResponse> Items { get; set; } = new();
}

