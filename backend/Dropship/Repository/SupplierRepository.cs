using Microsoft.Extensions.Caching.Memory;
using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Dropship.Requests;

namespace Dropship.Repository;

public class SupplierRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private const string SupplierCacheKeyPrefix = "supplier_";

    public SupplierRepository(DynamoDbRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }
    
    public async Task<SupplierDomain?> GetSupplierAsync(string supplierId)
    {
        var cacheKey = $"{SupplierCacheKeyPrefix}{supplierId}";
        
        if (_cache.TryGetValue(cacheKey, out SupplierDomain? cachedSupplier))
            return cachedSupplier;

        var key = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
            { "SK", new AttributeValue { S = "META" } }
        };

        var item = await _repository.GetItemAsync(key);
        if (item == null) return null;

        var supplier = SupplierMapper.ToDomain(item);
        _cache.Set(cacheKey, supplier, _cacheExpiration);
        
        return supplier;
    }

    /// <summary>
    /// Cria um novo fornecedor com todos os campos
    /// </summary>
    public async Task<SupplierDomain> CreateSupplierAsync(CreateSupplierRequest request)
    {
        var supplierId = Guid.NewGuid().ToString();
        var createdAtUtc = DateTime.UtcNow.ToString("O"); // ISO 8601 format
        
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
            { "SK", new AttributeValue { S = "META" } },
            { "id", new AttributeValue { S = supplierId } },
            { "entity_type", new AttributeValue { S = "supplier" } },
            { "name", new AttributeValue { S = request.Name } },
            { "legal_name", new AttributeValue { S = request.LegalName } },
            { "phone", new AttributeValue { S = request.Phone } },
            { "address", new AttributeValue { S = request.Address } },
            { "address_number", new AttributeValue { S = request.AddressNumber } },
            { "address_district", new AttributeValue { S = request.AddressDistrict } },
            { "address_city", new AttributeValue { S = request.AddressCity } },
            { "address_state", new AttributeValue { S = request.AddressState } },
            { "address_zipcode", new AttributeValue { S = request.AddressZipcode } },
            { "cnpj", new AttributeValue { S = request.Cnpj } },
            { "cst_csosn", new AttributeValue { S = request.CstCsosn } },
            { "created_at", new AttributeValue { S = createdAtUtc } }
        };

        // Adicionar eNota ID se fornecido
        if (!string.IsNullOrWhiteSpace(request.EnotasId))
        {
            item.Add("enotas_Id", new AttributeValue { S = request.EnotasId });
        }

        // Adicionar prioridade se fornecida
        if (request.Priority > 0)
        {
            item.Add("supplier_priority", new AttributeValue { S = request.Priority.ToString() });
        }

        await _repository.PutItemAsync(item);

        // Invalidar cache se existir
        _cache.Remove($"{SupplierCacheKeyPrefix}{supplierId}");

        return SupplierMapper.ToDomain(item);
    }

    /// <summary>
    /// Atualiza um fornecedor existente (created_at não pode ser modificado)
    /// </summary>
    public async Task<SupplierDomain?> UpdateSupplierAsync(string supplierId, UpdateSupplierRequest request)
    {
        var existingSupplier = await GetSupplierAsync(supplierId);
        if (existingSupplier == null)
            return null;

        // Construir item atualizado com valores existentes como fallback
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = existingSupplier.Pk } },
            { "SK", new AttributeValue { S = existingSupplier.Sk } },
            { "id", new AttributeValue { S = supplierId } },
            { "entity_type", new AttributeValue { S = existingSupplier.EntityType } },
            { "name", new AttributeValue { S = request.Name ?? existingSupplier.Name } },
            { "legal_name", new AttributeValue { S = request.LegalName ?? existingSupplier.LegalName } },
            { "phone", new AttributeValue { S = request.Phone ?? existingSupplier.Phone } },
            { "address", new AttributeValue { S = request.Address ?? existingSupplier.Address } },
            { "address_number", new AttributeValue { S = request.AddressNumber ?? existingSupplier.AddressNumber } },
            { "address_district", new AttributeValue { S = request.AddressDistrict ?? existingSupplier.AddressDistrict } },
            { "address_city", new AttributeValue { S = request.AddressCity ?? existingSupplier.AddressCity } },
            { "address_state", new AttributeValue { S = request.AddressState ?? existingSupplier.AddressState } },
            { "address_zipcode", new AttributeValue { S = request.AddressZipcode ?? existingSupplier.AddressZipcode } },
            { "cnpj", new AttributeValue { S = request.Cnpj ?? existingSupplier.Cnpj } },
            { "cst_csosn", new AttributeValue { S = request.CstCsosn ?? existingSupplier.CstCsosn } },
            { "created_at", new AttributeValue { S = existingSupplier.CreatedAt.ToString("O") } }, // Preservar original
            { "updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("O") } } // Adicionar updated_at
        };

        // Adicionar/Atualizar eNota ID se fornecido
        if (!string.IsNullOrWhiteSpace(request.EnotasId))
        {
            item["enotas_Id"] = new AttributeValue { S = request.EnotasId };
        }
        else if (!string.IsNullOrWhiteSpace(existingSupplier.EnotasId))
        {
            item["enotas_Id"] = new AttributeValue { S = existingSupplier.EnotasId };
        }

        // Adicionar/Atualizar prioridade
        var priority = request.Priority ?? existingSupplier.Priority;
        if (priority > 0)
        {
            item["supplier_priority"] = new AttributeValue { S = priority.ToString() };
        }

        await _repository.PutItemAsync(item);

        // Invalidar cache
        _cache.Remove($"{SupplierCacheKeyPrefix}{supplierId}");

        return SupplierMapper.ToDomain(item);
    }

    /// <summary>
    /// Deleta um fornecedor
    /// </summary>
    public async Task<bool> DeleteSupplierAsync(string supplierId)
    {
        var key = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
            { "SK", new AttributeValue { S = "META" } }
        };

        var response = await _repository.DeleteItemAsync(key);
        
        // Invalidar cache se a operação foi bem-sucedida (status 200)
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            _cache.Remove($"{SupplierCacheKeyPrefix}{supplierId}");

        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    /// <summary>
    /// Lista todos os fornecedores usando GSI_RELATIONS_LOOKUP
    /// No GSI, SK é usado como PK (inverte a lógica) para query eficiente
    /// </summary>
    public async Task<List<SupplierDomain>> GetAllSuppliersAsync()
    {
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":sk", new AttributeValue { S = "META" } },
            { ":pk", new AttributeValue { S = "Supplier#" } }
        };

        var items = await _repository.QueryTableAsync(
            keyConditionExpression: "SK = :sk AND begins_with(PK, :pk)",
            expressionAttributeValues: expressionAttributeValues,
            indexName: "GSI_RELATIONS_LOOKUP"
        );

        if (items == null || items.Count == 0)
            return new List<SupplierDomain>();

        var suppliers = items
            .Select(SupplierMapper.ToDomain)
            .ToList();

        return suppliers;
    }
}


