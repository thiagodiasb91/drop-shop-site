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

    // Integração InfinityPay
    public string InfinityPayHandle { get; set; } = default!; // Username/Handle no InfinityPay
    
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

            // Integração InfinityPay
            InfinityPayHandle = item.ContainsKey("infinity_pay_handle") ? item["infinity_pay_handle"].S : "",
            
            // Metadata
            CreatedAt = createdAt,
            UpdatedAt = updatedAtString != null ? updatedAt : null
        };
    }

    /// <summary>
    /// Converte SupplierDomain para Dictionary pronto para salvar no DynamoDB
    /// </summary>
    public static Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> ToDynamoDb(this SupplierDomain domain)
    {
        var item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
        {
            { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Pk } },
            { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Sk } },
            { "id", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Id } },
            { "entity_type", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.EntityType } },
            { "name", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Name } },
            { "legal_name", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.LegalName } },
            { "phone", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Phone } },
            { "address", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Address } },
            { "address_number", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.AddressNumber } },
            { "address_district", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.AddressDistrict } },
            { "address_city", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.AddressCity } },
            { "address_state", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.AddressState } },
            { "address_zipcode", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.AddressZipcode } },
            { "cnpj", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Cnpj } },
            { "cst_csosn", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.CstCsosn } },
            { "created_at", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.CreatedAt.ToString("O") } }
        };

        // Adicionar eNota ID se fornecido
        if (!string.IsNullOrWhiteSpace(domain.EnotasId))
        {
            item["enotas_Id"] = new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.EnotasId };
        }

        // Adicionar InfinityPay Handle se fornecido
        if (!string.IsNullOrWhiteSpace(domain.InfinityPayHandle))
        {
            item["infinity_pay_handle"] = new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.InfinityPayHandle };
        }

        // Adicionar updated_at se fornecido
        if (domain.UpdatedAt.HasValue)
        {
            item["updated_at"] = new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.UpdatedAt.Value.ToString("O") };
        }

        return item;
    }
}




