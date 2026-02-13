namespace Dropship.Responses;

/// <summary>
/// Response para imagem de produto
/// </summary>
public class ProductImageResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ImageId { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string BrUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response para lista de imagens de produto
/// </summary>
public class ProductImagesListResponse
{
    public int Total { get; set; }
    public List<ProductImageResponse> Items { get; set; } = new();
}

/// <summary>
/// Mapper para ProductImage
/// </summary>
public static class ProductImageResponseMapper
{
    public static ProductImageResponse ToResponse(this Domain.ProductImageDomain domain)
    {
        return new ProductImageResponse
        {
            ProductId = domain.ProductId,
            ImageId = domain.ImageId,
            Color = domain.Color,
            ImageUrl = domain.ImageUrl,
            BrUrl = domain.BrUrl,
            CreatedAt = domain.CreatedAt
        };
    }

    public static ProductImagesListResponse ToListResponse(this List<Domain.ProductImageDomain> domains)
    {
        return new ProductImagesListResponse
        {
            Total = domains.Count,
            Items = domains.Select(d => d.ToResponse()).ToList()
        };
    }
}

