namespace Dropship.Responses;

/// <summary>
/// Response para item individual na listagem de fornecedores
/// </summary>
public class SupplierItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response para listar fornecedores (container com paginação)
/// </summary>
public class SupplierListResponse
{
    /// <summary>
    /// Total de fornecedores
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Lista de fornecedores
    /// </summary>
    public List<SupplierItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Mapper para converter Domain em Response
/// </summary>
public static class SupplierListResponseMapper
{
    public static SupplierItemResponse ToItemResponse(this Domain.SupplierDomain supplier)
    {
        return new SupplierItemResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            LegalName = supplier.LegalName,
            Phone = supplier.Phone,
            Priority = supplier.Priority,
            Cnpj = supplier.Cnpj,
            CreatedAt = supplier.CreatedAt
        };
    }

    public static SupplierListResponse ToListResponse(this List<Domain.SupplierDomain> suppliers)
    {
        return new SupplierListResponse
        {
            Total = suppliers.Count,
            Items = suppliers.Select(s => s.ToItemResponse()).ToList()
        };
    }
}
