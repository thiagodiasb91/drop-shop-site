using System.Text.Json;

namespace Dropship.Services.Helpers;

/// <summary>
/// Helper com métodos utilitários para integração com API da Shopee
/// Contém métodos estáticos para operações comuns
/// </summary>
public static class ShopeeApiHelper
{
    /// <summary>
    /// Obtém o timestamp Unix atual em segundos
    /// </summary>
    /// <returns>Timestamp Unix em segundos</returns>
    public static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Extrai valor de string de um JsonElement
    /// Retorna string vazia se a propriedade não existir
    /// </summary>
    /// <param name="doc">JsonDocument com a resposta</param>
    /// <param name="propertyName">Nome da propriedade a extrair</param>
    /// <returns>Valor da propriedade ou string vazia</returns>
    public static string GetJsonProperty(JsonDocument doc, string propertyName)
    {
        if (doc.RootElement.TryGetProperty(propertyName, out var element))
        {
            return element.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Parse do expires_in com fallback para diferentes nomes de propriedade
    /// Tenta múltiplas variações de nomes que a Shopee pode usar
    /// </summary>
    /// <param name="doc">JsonDocument com a resposta da Shopee</param>
    /// <returns>Tempo de expiração em segundos (default: 3600)</returns>
    public static long ParseExpiresIn(JsonDocument doc)
    {
        var expiresIn = 3600L; // Default: 1 hora

        // Tenta diferentes nomes de propriedade que a Shopee pode usar
        var propertyNames = new[] { "expires_in", "expire_in", "expire", "expired_in" };

        foreach (var propertyName in propertyNames)
        {
            if (doc.RootElement.TryGetProperty(propertyName, out var element))
            {
                if (element.TryGetInt64(out var value) && value > 0)
                {
                    return value;
                }
                else if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out var stringValue) && stringValue > 0)
                {
                    return stringValue;
                }
            }
        }

        return expiresIn;
    }
}
