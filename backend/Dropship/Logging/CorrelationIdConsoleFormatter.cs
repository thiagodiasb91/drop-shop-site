using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Dropship.Logging;

/// <summary>
/// Opções do formatter JSON estruturado
/// </summary>
public sealed class CorrelationIdFormatterOptions
{
    public bool UseUtcTimestamp { get; set; } = true;
    /// <summary>
    /// Nome do serviço incluído em todos os logs (facilita filtro no CloudWatch)
    /// Padrão: valor da env var SERVICE_NAME ou "dropship-api"
    /// </summary>
    public string ServiceName { get; set; } =
        Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "dropship-api";
}

/// <summary>
/// Console formatter que emite uma linha JSON por log entry.
/// Formato compatível com CloudWatch Logs Insights — cada campo é filtrável diretamente.
///
/// Exemplo de saída:
/// {
///   "timestamp": "2026-02-28T14:00:00.000Z",
///   "level": "INFO",
///   "correlationId": "abc-123",
///   "service": "dropship-api",
///   "category": "Dropship.Controllers.SellersController",
///   "message": "Getting seller...",
///   "exception": null
/// }
///
/// CloudWatch Logs Insights query:
///   fields @timestamp, level, correlationId, message
///   | filter level = "ERROR"
///   | filter correlationId = "abc-123"
/// </summary>
public sealed class CorrelationIdConsoleFormatter : ConsoleFormatter
{
    private readonly IDisposable? _optionsReloadToken;
    private CorrelationIdFormatterOptions _options;

    private static readonly JsonWriterOptions _jsonWriterOptions = new()
    {
        Indented = false,
        SkipValidation = true
    };

    public CorrelationIdConsoleFormatter(IOptionsMonitor<CorrelationIdFormatterOptions> options)
        : base(nameof(CorrelationIdConsoleFormatter))
    {
        _options = options.CurrentValue;
        _optionsReloadToken = options.OnChange(o => _options = o);
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty;

        if (string.IsNullOrEmpty(message) && logEntry.Exception == null)
            return;

        var correlationId = ExtractCorrelationId(scopeProvider);
        var timestamp = _options.UseUtcTimestamp
            ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            : DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");

        // Write compact JSON — one line per log entry for CloudWatch
        using var ms = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(ms, _jsonWriterOptions))
        {
            writer.WriteStartObject();

            writer.WriteString("timestamp",     timestamp);
            writer.WriteString("level",         GetLogLevelString(logEntry.LogLevel));
            writer.WriteString("correlationId", correlationId);
            writer.WriteString("service",       _options.ServiceName);
            writer.WriteString("category",      logEntry.Category);
            writer.WriteString("message",       message);

            // Structured log properties from the message template
            // e.g. _logger.LogInformation("Getting order {OrderId}", orderId)
            // → "OrderId": "abc-123"
            WriteStateProperties(writer, logEntry.State);

            // Extra scoped properties (besides CorrelationId)
            WriteScopeProperties(writer, scopeProvider);

            // Exception
            if (logEntry.Exception != null)
            {
                writer.WriteString("exception",      logEntry.Exception.GetType().FullName ?? "Exception");
                writer.WriteString("exceptionMsg",   logEntry.Exception.Message);
                writer.WriteString("stackTrace",     logEntry.Exception.StackTrace ?? string.Empty);
            }
            else
            {
                writer.WriteNull("exception");
            }

            writer.WriteEndObject();
        }

        textWriter.WriteLine(System.Text.Encoding.UTF8.GetString(ms.ToArray()));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extrai as propriedades nomeadas do template de log e as escreve como campos JSON.
    /// Exemplo: LogInformation("Order {OrderId} from {SellerId}", orderId, sellerId)
    ///          → "OrderId": "...", "SellerId": "..."
    /// A chave especial "{OriginalFormat}" é ignorada pois é o template em si.
    /// </summary>
    private static void WriteStateProperties<TState>(Utf8JsonWriter writer, TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object>> kvps)
            return;

        foreach (var kvp in kvps)
        {
            // {OriginalFormat} é o template da mensagem — não é uma propriedade de negócio
            if (kvp.Key == "{OriginalFormat}") continue;

            try
            {
                writer.WriteString(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
            }
            catch
            {
                // ignora chave duplicada ou inválida
            }
        }
    }

    private static string ExtractCorrelationId(IExternalScopeProvider? scopeProvider)
    {
        string? correlationId = null;

        scopeProvider?.ForEachScope((scope, _) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> kvps)
            {
                foreach (var kvp in kvps)
                {
                    if (kvp.Key == "CorrelationId")
                        correlationId = kvp.Value?.ToString();
                }
            }
        }, (object?)null);

        return correlationId ?? "NO-CORRELATION-ID";
    }

    private static void WriteScopeProperties(Utf8JsonWriter writer, IExternalScopeProvider? scopeProvider)
    {
        if (scopeProvider == null) return;

        scopeProvider.ForEachScope((scope, _) =>
        {
            if (scope is not IEnumerable<KeyValuePair<string, object>> kvps) return;

            foreach (var kvp in kvps)
            {
                // CorrelationId already written at top level
                if (kvp.Key == "CorrelationId") continue;

                try
                {
                    writer.WriteString(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
                }
                catch
                {
                    // ignore duplicate or invalid keys
                }
            }
        }, (object?)null);
    }

    private static string GetLogLevelString(LogLevel level) => level switch
    {
        LogLevel.Trace       => "TRACE",
        LogLevel.Debug       => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning     => "WARN",
        LogLevel.Error       => "ERROR",
        LogLevel.Critical    => "FATAL",
        _                    => "UNKN"
    };

    public void Dispose() => _optionsReloadToken?.Dispose();
}
