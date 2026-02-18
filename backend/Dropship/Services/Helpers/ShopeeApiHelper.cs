using System.Security.Cryptography;
using System.Text;
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
    /// Gera assinatura HMAC SHA256 para requisições à API (public endpoints)
    /// Base string: {partner_id}{path}{timestamp}
    /// </summary>
    /// <param name="partnerId">ID do parceiro Shopee</param>
    /// <param name="partnerKey">Chave do parceiro Shopee</param>
    /// <param name="path">Path da API (ex: /api/v2/shop/auth_partner)</param>
    /// <param name="timestamp">Timestamp Unix em segundos</param>
    /// <returns>Assinatura HMAC SHA256 em hexadecimal lowercase</returns>
    public static string GenerateSign(string partnerId, string partnerKey, string path, long timestamp)
    {
        var tmpBaseString = $"{partnerId}{path}{timestamp}";
        var baseString = Encoding.UTF8.GetBytes(tmpBaseString);
        var key = Encoding.UTF8.GetBytes(partnerKey);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(baseString);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Gera assinatura HMAC SHA256 para requisições à API (shop-level endpoints)
    /// Base string: {partner_id}{path}{timestamp}{access_token}{shop_id}
    /// Ref: https://open.shopee.com/documents/v2/v2.shop.get_shop_info
    /// </summary>
    /// <param name="partnerId">ID do parceiro Shopee</param>
    /// <param name="partnerKey">Chave do parceiro Shopee</param>
    /// <param name="path">Path da API (ex: /api/v2/shop/get_shop_info)</param>
    /// <param name="timestamp">Timestamp Unix em segundos</param>
    /// <param name="accessToken">Token de acesso da loja</param>
    /// <param name="shopId">ID da loja</param>
    /// <returns>Assinatura HMAC SHA256 em hexadecimal lowercase</returns>
    public static string GenerateSignWithShop(string partnerId, string partnerKey, string path, long timestamp, string accessToken, long shopId)
    {
        var tmpBaseString = $"{partnerId}{path}{timestamp}{accessToken}{shopId}";
        var baseString = Encoding.UTF8.GetBytes(tmpBaseString);
        var key = Encoding.UTF8.GetBytes(partnerKey);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(baseString);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
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
