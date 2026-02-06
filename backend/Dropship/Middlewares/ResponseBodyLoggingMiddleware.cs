using System.Text;

namespace Dropship.Middlewares;

/// <summary>
/// Middleware para logar o body completo das respostas HTTP
/// Reaproveitando o CorrelationId do CorrelationIdMiddleware
/// </summary>
public class ResponseBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseBodyLoggingMiddleware> _logger;

    public ResponseBodyLoggingMiddleware(RequestDelegate next, ILogger<ResponseBodyLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Armazenar o stream original
        var originalBodyStream = context.Response.Body;

        try
        {
            // Criar um stream em memória para capturar a resposta
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            // Chamar o próximo middleware/controller
            await _next(context);

            // Ler a resposta do stream
            memoryStream.Position = 0;
            var responseBody = await new StreamReader(memoryStream, Encoding.UTF8).ReadToEndAsync();

            // Logar a resposta
            if (!string.IsNullOrEmpty(responseBody) && context.Response.ContentLength > 0)
            {
                _logger.LogInformation(
                    "Response Body - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, ContentType: {ContentType}, Body: {Body}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    context.Response.ContentType,
                    responseBody
                );
            }
            else
            {
                _logger.LogInformation(
                    "Response Body - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, ContentType: {ContentType}, Body: (empty)",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    context.Response.ContentType
                );
            }

            // Copiar o stream capturado de volta ao stream original
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging response body - Path: {Path}", context.Request.Path);
            // Garantir que o stream original seja restaurado mesmo em caso de erro
            context.Response.Body = originalBodyStream;
            throw;
        }
        finally
        {
            // Restaurar o stream original
            context.Response.Body = originalBodyStream;
        }
    }
}
