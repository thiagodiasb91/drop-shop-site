using System.Text.Json;

namespace Dropship.Services;

/// <summary>
/// Serviço para comunicação com o cache remoto via API (AWS)
/// Baseado no padrão Python fornecido
/// Usado em ambiente de produção
/// </summary>
public class ApiCacheService : ICacheService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiCacheService> _logger;
    private const string CacheServiceUrl = "https://c069zuj7g8.execute-api.us-east-1.amazonaws.com/dev/cache";

    public ApiCacheService(HttpClient httpClient, ILogger<ApiCacheService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Obtém um único valor do cache via API
    /// </summary>
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            var result = await GetManyAsync(new[] { key });
            return result.ContainsKey(key) ? result[key] : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache - Key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Obtém múltiplos valores do cache via API
    /// Retorna um dicionário com os pares key/value
    /// Se um valor não existir, a key não será incluída no resultado
    /// Usa GET com keys no body (custom GET request)
    /// </summary>
    public async Task<Dictionary<string, string?>> GetManyAsync(params string[] keys)
    {
        try
        {
            _logger.LogInformation("Cache API GetMany - Keys: {Keys}", string.Join(", ", keys));

            // Criar body com as keys
            var body = new { keys };
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Cache API GetMany Request Body: {Body}", JsonSerializer.Serialize(body));

            using var request = new HttpRequestMessage(HttpMethod.Get, CacheServiceUrl)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Cache API error - StatusCode: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                return new Dictionary<string, string?>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Cache API GetMany Response: {Response}", responseContent);
            
            var jsonDoc = JsonDocument.Parse(responseContent);

            var result = new Dictionary<string, string?>();

            // Parsear resposta: pode ser um objeto com chaves como propriedades
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    var value = property.Value.ValueKind == JsonValueKind.Null 
                        ? null 
                        : property.Value.GetString();
                    result[property.Name] = value;
                }
            }

            _logger.LogInformation("Cache API GetMany success - Returned {Count} items", result.Count);
            return result;
        }
        catch (HttpRequestException hreq)
        {
            _logger.LogError(hreq, "HTTP Request error getting values from cache - InnerException: {InnerException}",
                hreq.InnerException?.Message);
            return new Dictionary<string, string?>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting values from cache");
            return new Dictionary<string, string?>();
        }
    }

    /// <summary>
    /// Salva um único valor no cache via API
    /// </summary>
    public async Task<bool> SaveAsync(string key, string? value)
    {
        return await SaveManyAsync(new[] { (key, value) });
    }

    /// <summary>
    /// Salva múltiplos valores no cache via API
    /// Se value for null, deleta a chave
    /// </summary>
    public async Task<bool> SaveManyAsync(params (string key, string? value)[] keyValues)
    {
        try
        {
            var items = keyValues.Select(kv => new { kv.key, kv.value }).ToArray();
            var body = new { key_values = items };
            
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Cache API SaveMany - Items: {Count}", items.Length);

            var response = await _httpClient.PostAsync(CacheServiceUrl, content);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Cache API error - StatusCode: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("Cache API SaveMany success - {Count} items saved", items.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving values to cache");
            return false;
        }
    }

    /// <summary>
    /// Deleta uma chave do cache via API
    /// </summary>
    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            _logger.LogInformation("Deleting key from cache - Key: {Key}", key);
            
            // Deletar passando value como null
            return await SaveAsync(key, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key from cache - Key: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Verifica se uma chave existe no cache via API
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            _logger.LogInformation("Checking if key exists in cache - Key: {Key}", key);

            var value = await GetAsync(key);
            var exists = value != null;

            _logger.LogInformation("Key existence check - Key: {Key}, Exists: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence in cache - Key: {Key}", key);
            return false;
        }
    }
}
