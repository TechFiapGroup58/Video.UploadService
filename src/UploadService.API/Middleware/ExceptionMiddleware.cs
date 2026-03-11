using System.Net;
using System.Text.Json;
using UploadService.Core.Exceptions;

namespace UploadService.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled: {Msg}", ex.Message);
            await WriteAsync(ctx, ex);
        }
    }

    private static Task WriteAsync(HttpContext ctx, Exception ex)
    {
        var (status, title) = ex switch
        {
            VideoUploadNotFoundException                          => (HttpStatusCode.NotFound,      "Não encontrado"),
            InvalidVideoException                                => (HttpStatusCode.BadRequest,     "Requisição inválida"),
            UnauthorizedUploadException or UnauthorizedAccessException
                                                                 => (HttpStatusCode.Forbidden,     "Acesso negado"),
            StorageException                                     => (HttpStatusCode.BadGateway,    "Erro de armazenamento"),
            _                                                    => (HttpStatusCode.InternalServerError, "Erro interno")
        };

        ctx.Response.StatusCode  = (int)status;
        ctx.Response.ContentType = "application/problem+json";

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type    = $"https://httpstatuses.com/{(int)status}",
            title,
            status  = (int)status,
            detail  = ex.Message,
            traceId = ctx.TraceIdentifier
        }, JsonOpts));
    }
}
