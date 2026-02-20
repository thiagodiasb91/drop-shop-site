namespace Dropship.Services;

/// <summary>
/// Interface para serviço de cache
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtém um único valor do cache
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Obtém múltiplos valores do cache
    /// </summary>
    Task<Dictionary<string, string?>> GetManyAsync(params string[] keys);

    /// <summary>
    /// Salva um único valor no cache
    /// </summary>
    Task<bool> SaveAsync(string key, string? value);

    /// <summary>
    /// Salva múltiplos valores no cache
    /// </summary>
    Task<bool> SaveManyAsync(params (string key, string? value)[] keyValues);

    /// <summary>
    /// Deleta uma chave do cache
    /// </summary>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// Verifica se uma chave existe no cache
    /// </summary>
    Task<bool> ExistsAsync(string key);
}

