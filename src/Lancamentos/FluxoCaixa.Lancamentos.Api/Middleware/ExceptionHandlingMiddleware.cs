using System.Net;
using System.Text.Json;
using FluentValidation;
using FluxoCaixa.Lancamentos.Domain.Exceptions;

namespace FluxoCaixa.Lancamentos.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Erro de validação", ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (DomainException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Erro de regra de negócio", new[] { ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar a requisição.");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Erro interno", new[] { "Ocorreu um erro inesperado." });
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, IEnumerable<string> errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { title, errors });
        await context.Response.WriteAsync(payload);
    }
}
