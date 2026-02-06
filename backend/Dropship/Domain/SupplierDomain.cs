namespace Dropship.Domain;

public class SupplierDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!;
    public string Sk { get; set; } = default!;
    
    // Identificadores
    public string Id { get; set; } = default!;
    public string EntityType { get; set; } = "supplier";
    
    // Informações Básicas
    public string Name { get; set; } = default!;
    public string LegalName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public int Priority { get; set; }
    
    // Endereço
    public string Address { get; set; } = default!;
    public string AddressNumber { get; set; } = default!;
    public string AddressDistrict { get; set; } = default!;
    public string AddressCity { get; set; } = default!;
    public string AddressState { get; set; } = default!;
    public string AddressZipcode { get; set; } = default!;
    
    // Dados Fiscais
    public string Cnpj { get; set; } = default!;
    public string CstCsosn { get; set; } = default!;
    
    // Integração eNota
    public string EnotasId { get; set; } = default!;
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public static class SupplierMapper
{
    public static SupplierDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        // Parsear CreatedAt - formato ISO 8601
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);
        
        // Parsear UpdatedAt
        var updatedAtString = item.ContainsKey("updated_at") ? item["updated_at"].S : null;
        DateTime.TryParse(updatedAtString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var updatedAt);

        return new SupplierDomain
        {
            // Chaves
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",
            
            // Identificadores
            Id = item.ContainsKey("id") ? item["id"].S : (item.ContainsKey("supplier_id") ? item["supplier_id"].S : ""),
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "supplier",
            
            // Informações Básicas
            Name = item.ContainsKey("name") ? item["name"].S : (item.ContainsKey("supplier_name") ? item["supplier_name"].S : ""),
            LegalName = item.ContainsKey("legal_name") ? item["legal_name"].S : "",
            Phone = item.ContainsKey("phone") ? item["phone"].S : "",
            Priority = int.TryParse(item.ContainsKey("supplier_priority") ? item["supplier_priority"].S : "0", out var priority) ? priority : 0,
            
            // Endereço
            Address = item.ContainsKey("address") ? item["address"].S : "",
            AddressNumber = item.ContainsKey("address_number") ? item["address_number"].S : "",
            AddressDistrict = item.ContainsKey("address_district") ? item["address_district"].S : "",
            AddressCity = item.ContainsKey("address_city") ? item["address_city"].S : "",
            AddressState = item.ContainsKey("address_state") ? item["address_state"].S : "",
            AddressZipcode = item.ContainsKey("address_zipcode") ? item["address_zipcode"].S : "",
            
            // Dados Fiscais
            Cnpj = item.ContainsKey("cnpj") ? item["cnpj"].S : "",
            CstCsosn = item.ContainsKey("cst_csosn") ? item["cst_csosn"].S : "",
            
            // Integração eNota
            EnotasId = item.ContainsKey("enotas_Id") ? item["enotas_Id"].S : "",
            
            // Metadata
            CreatedAt = createdAt,
            UpdatedAt = updatedAtString != null ? updatedAt : null
        };
    }
}




