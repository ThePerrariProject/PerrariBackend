using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Perrari.Application.Common.Exceptions;
using Perrari.Domain.Exceptions;

namespace Perrari.Api.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        int status;
        string title;
        IDictionary<string, string[]>? errors = null;

        switch (ex)
        {
            case ValidationException ve:
                status = StatusCodes.Status400BadRequest;
                title = ve.Errors.FirstOrDefault()?.ErrorMessage ?? "Datos Invalidos";
                errors = ve.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case UnauthorizedException:
                status = StatusCodes.Status401Unauthorized;
                title = ex.Message;
                break;

            case AccountLockedException:
                status = StatusCodes.Status423Locked;
                title = ex.Message;
                break;

            case ConflictException:
                status = StatusCodes.Status409Conflict;
                title = ex.Message;
                break;

            case DomainException:
                status = StatusCodes.Status422UnprocessableEntity;
                title = ex.Message;
                break;

            default:
                logger.LogError(ex, "Error no encontrado en {Method} {Path}", context.Request.Method, context.Request.Path);
                status = StatusCodes.Status500InternalServerError;
                title = "Ocurrio un error inesperado";
                break;
        }

        var problem = new ProblemDetails { Status = status, Title = title };
        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }
}