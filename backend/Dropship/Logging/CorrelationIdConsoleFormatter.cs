using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Text;

namespace Dropship.Logging;

/// <summary>
/// Custom Console Formatter que coloca CorrelationId no início de cada mensagem
/// Formato: CorrelationId: {id} - [Timestamp] [Level] [Category] Message
/// </summary>
public sealed class CorrelationIdFormatterOptions
{
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
    public bool IncludeScopes { get; set; } = true;
    public bool UseUtcTimestamp { get; set; } = false;
}

public sealed class CorrelationIdConsoleFormatter : ConsoleFormatter
{
    private readonly IDisposable? _optionsReloadToken;
    private CorrelationIdFormatterOptions _options = new();

    public CorrelationIdConsoleFormatter(IOptionsMonitor<CorrelationIdFormatterOptions> options)
        : base(nameof(CorrelationIdConsoleFormatter))
    {
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        _options = options.CurrentValue;
    }

    private void ReloadLoggerOptions(CorrelationIdFormatterOptions options)
    {
        _options = options;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? "";
        
        if (string.IsNullOrEmpty(message) && logEntry.Exception == null)
            return;

        // Extrair CorrelationId
        var correlationId = ExtractCorrelationId(scopeProvider);
        
        // Construir mensagem formatada
        var sb = new StringBuilder();

        // CorrelationId no início
        sb.Append($"CorrelationId: {correlationId} - ");

        // Timestamp
        var timestamp = _options.UseUtcTimestamp 
            ? DateTime.UtcNow.ToString(_options.TimestampFormat)
            : DateTime.Now.ToString(_options.TimestampFormat);
        sb.Append($"[{timestamp}] ");

        // Log Level
        var level = GetLogLevelString(logEntry.LogLevel);
        sb.Append($"[{level}] ");

        // Category
        sb.Append($"[{logEntry.Category}] ");

        // Message
        sb.AppendLine(message);

        // Exception se houver
        if (logEntry.Exception != null)
        {
            sb.AppendLine();
            sb.AppendLine("Exception:");
            sb.AppendLine(logEntry.Exception.ToString());
        }

        textWriter.Write(sb.ToString());
    }

    private static string ExtractCorrelationId(IExternalScopeProvider? scopeProvider)
    {
        string? correlationId = null;

        if (scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, state) =>
            {
                if (scope is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("CorrelationId", out var id))
                    {
                        correlationId = id?.ToString();
                    }
                }
            }, (object?)null);
        }

        return !string.IsNullOrEmpty(correlationId) ? correlationId : "NO-CORRELATION-ID";
    }

    private static string GetLogLevelString(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO ",
        LogLevel.Warning => "WARN ",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "FATAL",
        _ => "UNKN "
    };
}
