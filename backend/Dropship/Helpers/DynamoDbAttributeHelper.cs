using Amazon.DynamoDBv2.Model;
using System.Globalization;

namespace Dropship.Helpers;

/// <summary>
/// Helpers de leitura para atributos do DynamoDB.
/// Todos os métodos são null-safe e retornam um valor default quando a chave não existe.
/// 
/// Uso:
///   item.GetS("key")           → string
///   item.GetS("key", "def")    → string com default
///   item.GetN&lt;long&gt;("key")    → número
///   item.GetDecimal("key")     → decimal (InvariantCulture)
///   item.GetBool("key")        → bool
///   item.GetDateTimeS("key")   → DateTime a partir de string ISO-8601
///   item.GetDateTimeN("key")   → DateTime a partir de Unix timestamp (N)
///   item.GetList("key")        → List&lt;AttributeValue&gt;
/// </summary>
public static class DynamoDbAttributeHelper
{
    // ── String ────────────────────────────────────────────────────────────────

    public static string GetS(this Dictionary<string, AttributeValue> item, string key, string defaultValue = "")
        => item.TryGetValue(key, out var v) ? v.S ?? defaultValue : defaultValue;

    public static string? GetSNullable(this Dictionary<string, AttributeValue> item, string key)
        => item.TryGetValue(key, out var v) ? v.S : null;

    // ── Number ────────────────────────────────────────────────────────────────

    public static T GetN<T>(this Dictionary<string, AttributeValue> item, string key, T defaultValue = default!)
    {
        if (!item.TryGetValue(key, out var v) || string.IsNullOrEmpty(v.N))
            return defaultValue;

        return (T)Convert.ChangeType(v.N, typeof(T), CultureInfo.InvariantCulture);
    }

    public static decimal GetDecimal(this Dictionary<string, AttributeValue> item, string key, decimal defaultValue = 0m)
    {
        if (!item.TryGetValue(key, out var v) || string.IsNullOrEmpty(v.N))
            return defaultValue;

        return decimal.TryParse(v.N, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    // ── Bool ──────────────────────────────────────────────────────────────────

    public static bool GetBool(this Dictionary<string, AttributeValue> item, string key, bool defaultValue = false)
        => item.TryGetValue(key, out var v) ? v.BOOL ?? defaultValue : defaultValue;

    // ── DateTime ──────────────────────────────────────────────────────────────

    /// <summary>Lê DateTime a partir de uma string ISO-8601 armazenada como atributo S.</summary>
    public static DateTime GetDateTimeS(this Dictionary<string, AttributeValue> item, string key, DateTime? defaultValue = null)
    {
        var fallback = defaultValue ?? DateTime.UtcNow;

        if (!item.TryGetValue(key, out var v) || string.IsNullOrEmpty(v.S))
            return fallback;

        return DateTime.TryParse(v.S, null, DateTimeStyles.RoundtripKind, out var result) ? result : fallback;
    }

    /// <summary>Lê DateTime? a partir de uma string ISO-8601 (retorna null quando ausente).</summary>
    public static DateTime? GetDateTimeSNullable(this Dictionary<string, AttributeValue> item, string key)
    {
        if (!item.TryGetValue(key, out var v) || string.IsNullOrEmpty(v.S))
            return null;

        return DateTime.TryParse(v.S, null, DateTimeStyles.RoundtripKind, out var result) ? result : null;
    }

    /// <summary>Lê DateTime a partir de um Unix timestamp armazenado como atributo N.</summary>
    public static DateTime GetDateTimeN(this Dictionary<string, AttributeValue> item, string key, DateTime? defaultValue = null)
    {
        var fallback = defaultValue ?? DateTime.UtcNow;

        if (!item.TryGetValue(key, out var v) || string.IsNullOrEmpty(v.N))
            return fallback;

        return long.TryParse(v.N, out var ts)
            ? DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime
            : fallback;
    }

    /// <summary>Lê long? a partir de um Unix timestamp armazenado como atributo N.</summary>
    public static long? GetUnixTimestampNullable(this Dictionary<string, AttributeValue> item, string key)
    {
        if (!item.TryGetValue(key, out var v) || string.IsNullOrEmpty(v.N))
            return null;

        return long.TryParse(v.N, out var result) ? result : null;
    }

    // ── List / Map ────────────────────────────────────────────────────────────

    /// <summary>Retorna a lista de AttributeValue de um atributo L.</summary>
    public static List<AttributeValue> GetList(this Dictionary<string, AttributeValue> item, string key)
        => item.TryGetValue(key, out var v) && v.L != null ? v.L : new List<AttributeValue>();

    /// <summary>Retorna o Map de AttributeValue de um atributo M.</summary>
    public static Dictionary<string, AttributeValue> GetMap(this Dictionary<string, AttributeValue> item, string key)
        => item.TryGetValue(key, out var v) && v.M != null ? v.M : new Dictionary<string, AttributeValue>();
}

