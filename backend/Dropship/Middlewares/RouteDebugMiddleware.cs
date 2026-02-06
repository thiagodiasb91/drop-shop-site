using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class RouteDebugMiddleware
{
    private readonly RequestDelegate _next;

    public RouteDebugMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            var path = context.Request.Path.Value;
            var method = context.Request.Method;

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync($$"""
                                                {
                                                  "error": "404 - rota nao encontrada",
                                                  "method": "{{method}}",
                                                  "pathRecebido": "{{path}}"
                                                }
                                                """);
        }
    }
}