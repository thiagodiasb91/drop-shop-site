using System.Text;

namespace Dropship.Middlewares;

/// <summary>
/// Middleware para logar o body completo das requisições
/// Reaproveitando o CorrelationId do CorrelationIdMiddleware
/// </summary>
public class RequestBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestBodyLoggingMiddleware> _logger;

    public RequestBodyLoggingMiddleware(RequestDelegate next, ILogger<RequestBodyLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Permitir releitura do body
        context.Request.EnableBuffering();

        // Ler o body
        var body = await ReadRequestBodyAsync(context.Request);

        // Ressetar a posição do stream para que o próximo middleware/controller possa ler
        context.Request.Body.Position = 0;

        // Logar o body (apenas para métodos que podem ter body)
        if (!string.IsNullOrEmpty(body) && context.Request.ContentLength > 0)
        {
            _logger.LogInformation(
                "Request Body - Method: {Method}, Path: {Path}, ContentType: {ContentType}, Body: {Body}",
                context.Request.Method,
                context.Request.Path,
                context.Request.ContentType,
                body
            );
        }
        else if (context.Request.ContentLength == 0)
        {
            _logger.LogInformation(
                "Request Body - Method: {Method}, Path: {Path}, ContentType: {ContentType}, Body: (empty)",
                context.Request.Method,
                context.Request.Path,
                context.Request.ContentType
            );
        }

        await _next(context);
    }

    /// <summary>
    /// Lê o body da requisição de forma segura
    /// </summary>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            // Se não há conteúdo, retorna vazio
            if (request.ContentLength == null || request.ContentLength == 0)
            {
                return string.Empty;
            }

            // Ler o body
            using var reader = new StreamReader(request.Body, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            return body;
        }
        catch (Exception ex)
        {
            // Se falhar ao ler, logar o erro mas não quebrar a requisição
            return $"[Error reading body: {ex.Message}]";
        }
    }
}
