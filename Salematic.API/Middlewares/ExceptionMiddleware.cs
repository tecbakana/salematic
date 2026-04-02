using System.Net;
using System.Text.Json;

namespace Salematic.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = _env.IsDevelopment()
                ? new { erro = ex.Message, detalhe = ex.StackTrace }
                : (object)new { erro = "Ocorreu um erro interno. Tente novamente mais tarde." };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
