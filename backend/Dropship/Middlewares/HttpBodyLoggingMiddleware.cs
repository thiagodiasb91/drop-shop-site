using System.Text;

namespace Dropship.Middlewares;

/// <summary>
/// Middleware unificado para logar request e response bodies
/// Combina a funcionalidade de RequestBodyLoggingMiddleware e ResponseBodyLoggingMiddleware
/// Reaproveitando o CorrelationId do CorrelationIdMiddleware
/// </summary>
public class HttpBodyLoggingMiddleware(RequestDelegate next, ILogger<HttpBodyLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Logar Request Body
        await LogRequestBodyAsync(context);

        // Capturar Response Body
        var originalBodyStream = context.Response.Body;

        try
        {
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await next(context);

            // Logar Response Body
            await LogResponseBodyAsync(context, memoryStream);

            // Copiar o stream capturado de volta ao stream original
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in HTTP body logging middleware - Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);
            context.Response.Body = originalBodyStream;
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Loga o body da requisição
    /// </summary>
    private async Task LogRequestBodyAsync(HttpContext context)
    {
        try
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
                logger.LogInformation(
                    "[REQUEST] Method: {Method}, Path: {Path}, Body: {Body}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.ContentType,
                    body
                );
            }
            else
            {
                logger.LogInformation(
                    "[REQUEST] Method: {Method}, Path: {Path}, Body: (empty)",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.ContentType
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error reading request body - Path: {Path}, Method: {Method}", context.Request.Path, context.Request.Method);
        }
    }

    /// <summary>
    /// Loga o body da resposta
    /// </summary>
    private async Task LogResponseBodyAsync(HttpContext context, MemoryStream memoryStream)
    {
        try
        {
            memoryStream.Position = 0;
            var responseBody = await new StreamReader(memoryStream, Encoding.UTF8).ReadToEndAsync();

            if (!string.IsNullOrEmpty(responseBody))
            {
                logger.LogInformation(
                    "[RESPONSE] Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, ContentType: {ContentType}, Body: {Body}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    context.Response.ContentType,
                    responseBody
                );
            }
            else
            {
                logger.LogInformation(
                    "[RESPONSE] Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, ContentType: {ContentType}, Body: (empty)",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    context.Response.ContentType
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error reading response body - Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);
        }
    }

    /// <summary>
    /// Lê o body da requisição de forma segura
    /// </summary>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            if (request.ContentLength is null or 0)
                return string.Empty;

            using var reader = new StreamReader(request.Body, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            return body;
        }
        catch (Exception ex)
        {
            return $"[Error reading body: {ex.Message}]";
        }
    }
}

