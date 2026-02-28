using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

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
    public static SupplierDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new SupplierDomain
        {
            Pk              = item.GetS("PK"),
            Sk              = item.GetS("SK"),
            Id              = item.GetS("id") is { Length: > 0 } id ? id : item.GetS("supplier_id"),
            EntityType      = item.GetS("entity_type", "supplier"),
            Name            = item.GetS("name") is { Length: > 0 } n ? n : item.GetS("supplier_name"),
            LegalName       = item.GetS("legal_name"),
            Phone           = item.GetS("phone"),
            Address         = item.GetS("address"),
            AddressNumber   = item.GetS("address_number"),
            AddressDistrict = item.GetS("address_district"),
            AddressCity     = item.GetS("address_city"),
            AddressState    = item.GetS("address_state"),
            AddressZipcode  = item.GetS("address_zipcode"),
            Cnpj            = item.GetS("cnpj"),
            CstCsosn        = item.GetS("cst_csosn"),
            EnotasId        = item.GetS("enotas_Id"),
            InfinityPayHandle = item.GetS("infinity_pay_handle"),
            CreatedAt       = item.GetDateTimeS("created_at"),
            UpdatedAt       = item.GetDateTimeSNullable("updated_at"),
        };
    }

    /// <summary>
    /// Converte SupplierDomain para Dictionary pronto para salvar no DynamoDB
    /// </summary>
    public static Dictionary<string, AttributeValue> ToDynamoDb(this SupplierDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.Pk } },
            { "SK", new AttributeValue { S = domain.Sk } },
            { "id", new AttributeValue { S = domain.Id } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "name", new AttributeValue { S = domain.Name } },
            { "legal_name", new AttributeValue { S = domain.LegalName } },
            { "phone", new AttributeValue { S = domain.Phone } },
            { "address", new AttributeValue { S = domain.Address } },
            { "address_number", new AttributeValue { S = domain.AddressNumber } },
            { "address_district", new AttributeValue { S = domain.AddressDistrict } },
            { "address_city", new AttributeValue { S = domain.AddressCity } },
            { "address_state", new AttributeValue { S = domain.AddressState } },
            { "address_zipcode", new AttributeValue { S = domain.AddressZipcode } },
            { "cnpj", new AttributeValue { S = domain.Cnpj } },
            { "cst_csosn", new AttributeValue { S = domain.CstCsosn } },
            { "created_at", new AttributeValue { S = domain.CreatedAt.ToString("O") } }
        };

        // Adicionar eNota ID se fornecido
        if (!string.IsNullOrWhiteSpace(domain.EnotasId))
        {
            item["enotas_Id"] = new AttributeValue { S = domain.EnotasId };
        }

        // Adicionar InfinityPay Handle se fornecido
        if (!string.IsNullOrWhiteSpace(domain.InfinityPayHandle))
        {
            item["infinity_pay_handle"] = new AttributeValue { S = domain.InfinityPayHandle };
        }

        // Adicionar updated_at se fornecido
        if (domain.UpdatedAt.HasValue)
        {
            item["updated_at"] = new AttributeValue { S = domain.UpdatedAt.Value.ToString("O") };
        }

        return item;
    }
}
