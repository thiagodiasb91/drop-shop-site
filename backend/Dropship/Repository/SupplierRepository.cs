using Microsoft.Extensions.Caching.Memory;
using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Dropship.Requests;

namespace Dropship.Repository;

public class SupplierRepository(DynamoDbRepository repository, IMemoryCache cache)
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private const string SupplierCacheKeyPrefix = "supplier_";

    public async Task<SupplierDomain?> GetSupplierAsync(string supplierId)
    {
        var cacheKey = $"{SupplierCacheKeyPrefix}{supplierId}";
        
        if (cache.TryGetValue(cacheKey, out SupplierDomain? cachedSupplier))
            return cachedSupplier;

        var key = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"Supplier#{supplierId}" } },
            { "SK", new AttributeValue { S = "META" } }
        };

        var item = await repository.GetItemAsync(key);
        if (item == null) return null;

        var supplier = SupplierMapper.ToDomain(item);
        cache.Set(cacheKey, supplier, _cacheExpiration);
        
        return supplier;
    }

    /// <summary>
    /// Cria um novo fornecedor com todos os campos
    /// </summary>
    public async Task<SupplierDomain> CreateSupplierAsync(CreateSupplierRequest request)
    {
        var supplierId = Guid.NewGuid().ToString();
        
        var supplier = new SupplierDomain
        {
            Pk = $"Supplier#{supplierId}",
            Sk = "META",
            Id = supplierId,
            EntityType = "supplier",
            Name = request.Name,
            LegalName = request.LegalName,
            Phone = request.Phone,
            Address = request.Address,
            AddressNumber = request.AddressNumber,
            AddressDistrict = request.AddressDistrict,
            AddressCity = request.AddressCity,
            AddressState = request.AddressState,
            AddressZipcode = request.AddressZipcode,
            Cnpj = request.Cnpj,
            CstCsosn = request.CstCsosn,
            EnotasId = request.EnotasId ?? string.Empty,
            InfinityPayHandle = request.InfinityPayHandle ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var item = supplier.ToDynamoDb();
        await repository.PutItemAsync(item);

        // Invalidar cache se existir
        cache.Remove($"{SupplierCacheKeyPrefix}{supplierId}");

        return supplier;
    }

    /// <summary>
    /// Atualiza um fornecedor existente (created_at não pode ser modificado)
    /// </summary>
    public async Task<SupplierDomain?> UpdateSupplierAsync(string supplierId, UpdateSupplierRequest request)
    {
        var existingSupplier = await GetSupplierAsync(supplierId);
        if (existingSupplier == null)
            return null;

        // Atualizar campos com novos valores ou manter existentes
        var updatedSupplier = new SupplierDomain
        {
            Pk = existingSupplier.Pk,
            Sk = existingSupplier.Sk,
            Id = supplierId,
            EntityType = existingSupplier.EntityType,
            Name = request.Name ?? existingSupplier.Name,
            LegalName = request.LegalName ?? existingSupplier.LegalName,
            Phone = request.Phone ?? existingSupplier.Phone,
            Address = request.Address ?? existingSupplier.Address,
            AddressNumber = request.AddressNumber ?? existingSupplier.AddressNumber,
            AddressDistrict = request.AddressDistrict ?? existingSupplier.AddressDistrict,
            AddressCity = request.AddressCity ?? existingSupplier.AddressCity,
            AddressState = request.AddressState ?? existingSupplier.AddressState,
            AddressZipcode = request.AddressZipcode ?? existingSupplier.AddressZipcode,
            Cnpj = request.Cnpj ?? existingSupplier.Cnpj,
            CstCsosn = request.CstCsosn ?? existingSupplier.CstCsosn,
            EnotasId = request.EnotasId ?? existingSupplier.EnotasId,
            InfinityPayHandle = request.InfinityPayHandle ?? existingSupplier.InfinityPayHandle,
            CreatedAt = existingSupplier.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        var item = updatedSupplier.ToDynamoDb();
        await repository.PutItemAsync(item);

        // Invalidar cache
        cache.Remove($"{SupplierCacheKeyPrefix}{supplierId}");

        return updatedSupplier;
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

        var response = await repository.DeleteItemAsync(key);
        
        // Invalidar cache se a operação foi bem-sucedida (status 200)
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            cache.Remove($"{SupplierCacheKeyPrefix}{supplierId}");

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

        var items = await repository.QueryTableAsync(
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


