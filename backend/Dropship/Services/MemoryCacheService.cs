using System.Text.Json;

namespace Dropship.Services;

/// <summary>
/// Implementação do cache usando arquivo local (para desenvolvimento/debug)
/// Persiste o cache em um arquivo JSON para reaproveitar entre reinicializações
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly string _cacheFilePath;
    private readonly object _fileLock = new();
    private Dictionary<string, string?> _cache;

    public MemoryCacheService(ILogger<MemoryCacheService> logger)
    {
        _logger = logger;
        
        // Define o caminho do arquivo de cache no diretório temporário do usuário
        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dropship-cache");
        Directory.CreateDirectory(cacheDir);
        _cacheFilePath = Path.Combine(cacheDir, "local-cache.json");
        
        _logger.LogInformation("FileCache initialized - Path: {Path}", _cacheFilePath);
        
        // Carrega o cache existente do arquivo
        _cache = LoadCacheFromFile();
    }

    private Dictionary<string, string?> LoadCacheFromFile()
    {
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                var cache = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
                _logger.LogInformation("FileCache loaded - {Count} items", cache?.Count ?? 0);
                return cache ?? new Dictionary<string, string?>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading cache from file, starting with empty cache");
        }
        
        return new Dictionary<string, string?>();
    }

    private void SaveCacheToFile()
    {
        try
        {
            lock (_fileLock)
            {
                var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_cacheFilePath, json);
                _logger.LogDebug("FileCache saved to disk - {Count} items", _cache.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cache to file");
        }
    }

    public Task<string?> GetAsync(string key)
    {
        try
        {
            _logger.LogDebug("FileCache Get - Key: {Key}", key);
            _cache.TryGetValue(key, out var value);
            _logger.LogDebug("FileCache Get Result - Key: {Key}, Found: {Found}", key, value != null);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from file cache - Key: {Key}", key);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<Dictionary<string, string?>> GetManyAsync(params string[] keys)
    {
        var result = new Dictionary<string, string?>();
        
        try
        {
            _logger.LogDebug("FileCache GetMany - Keys: {Keys}", string.Join(", ", keys));
            
            foreach (var key in keys)
            {
                _cache.TryGetValue(key, out var value);
                result[key] = value;
            }
            
            _logger.LogDebug("FileCache GetMany Result - Found {Count} items", result.Count(kv => kv.Value != null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting values from file cache");
        }
        
        return Task.FromResult(result);
    }

    public Task<bool> SaveAsync(string key, string? value)
    {
        try
        {
            _logger.LogDebug("FileCache Save - Key: {Key}", key);
            
            if (value == null)
            {
                _cache.Remove(key);
                _logger.LogDebug("FileCache Removed - Key: {Key}", key);
            }
            else
            {
                _cache[key] = value;
                _logger.LogDebug("FileCache Saved - Key: {Key}", key);
            }
            
            // Persiste no arquivo
            SaveCacheToFile();
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving value to file cache - Key: {Key}", key);
            return Task.FromResult(false);
        }
    }

    public Task<bool> SaveManyAsync(params (string key, string? value)[] keyValues)
    {
        try
        {
            _logger.LogDebug("FileCache SaveMany - Items: {Count}", keyValues.Length);
            
            foreach (var (key, value) in keyValues)
            {
                if (value == null)
                {
                    _cache.Remove(key);
                }
                else
                {
                    _cache[key] = value;
                }
            }
            
            // Persiste no arquivo uma única vez
            SaveCacheToFile();
            
            _logger.LogDebug("FileCache SaveMany Success - {Count} items saved", keyValues.Length);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving values to file cache");
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteAsync(string key)
    {
        try
        {
            _logger.LogDebug("FileCache Delete - Key: {Key}", key);
            _cache.Remove(key);
            SaveCacheToFile();
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key from file cache - Key: {Key}", key);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(string key)
    {
        _cache.TryGetValue(key, out var value);
        return Task.FromResult(value != null);
    }
}

