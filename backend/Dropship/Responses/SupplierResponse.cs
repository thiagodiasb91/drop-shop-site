namespace Dropship.Responses;

/// <summary>
/// Response completa para informações de um fornecedor (supplier)
/// Nota: PK e SK não são expostos na API
/// </summary>
public class SupplierResponse
{
    // Identificadores
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = "supplier";
    
    // Informações Básicas
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Priority { get; set; }
    
    // Endereço
    public string Address { get; set; } = string.Empty;
    public string AddressNumber { get; set; } = string.Empty;
    public string AddressDistrict { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string AddressState { get; set; } = string.Empty;
    public string AddressZipcode { get; set; } = string.Empty;
    
    // Dados Fiscais
    public string Cnpj { get; set; } = string.Empty;
    public string CstCsosn { get; set; } = string.Empty;
    
    // Integração eNota
    public string EnotasId { get; set; } = string.Empty;
    
    // Metadata (read-only)
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Mapper para converter Domain em Response
/// </summary>
public static class SupplierResponseMapper
{
    public static SupplierResponse ToResponse(this Domain.SupplierDomain supplier)
    {
        return new SupplierResponse
        {
            Id = supplier.Id,
            EntityType = supplier.EntityType,
            Name = supplier.Name,
            LegalName = supplier.LegalName,
            Phone = supplier.Phone,
            Priority = supplier.Priority,
            Address = supplier.Address,
            AddressNumber = supplier.AddressNumber,
            AddressDistrict = supplier.AddressDistrict,
            AddressCity = supplier.AddressCity,
            AddressState = supplier.AddressState,
            AddressZipcode = supplier.AddressZipcode,
            Cnpj = supplier.Cnpj,
            CstCsosn = supplier.CstCsosn,
            EnotasId = supplier.EnotasId,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }

    public static List<SupplierResponse> ToResponse(this List<Domain.SupplierDomain> suppliers)
    {
        return suppliers.Select(s => s.ToResponse()).ToList();
    }
}


