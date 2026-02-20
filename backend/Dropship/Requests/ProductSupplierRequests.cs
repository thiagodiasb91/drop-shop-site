using System.ComponentModel.DataAnnotations;

namespace Dropship.Requests;

/// <summary>
/// Request para vincular um fornecedor a um produto
/// </summary>
public class SupplierToProductRequest
{
    public List<SkuSupplierReference> Skus { get; set; } = new();
}

public class SupplierUpdateRequest
{
    public List<SupplierSkuUpdateReference> Skus { get; set; } = new();
}

public class SkuSupplierReference
{
    public string Sku { get; set; } = string.Empty;
    
    public string SkuSupplier { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
}

public class SupplierSkuUpdateReference
{
    public string Sku { get; set; } = string.Empty;
    
    public string SkuSupplier { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    public long? Quantity { get; set; }
}

public class UpdateSupplierPricingRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Production price must be greater than 0")]
    public decimal? price { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "Quantity cannot be negative")]
    public long? Quantity { get; set; }
    
    public string? SkuSupplier { get; set; }
}

