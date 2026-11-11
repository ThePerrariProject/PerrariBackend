using System.Diagnostics;
using System.Security.Claims;

namespace Perrari.Api.Middlewares;

public class AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var isMutation = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || 
                         HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method);

        if (!isMutation)
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        await next(context);

        sw.Stop();

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var path = context.Request.Path;
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? context.User?.FindFirst("sub")?.Value 
                     ?? "Anonimo";
        var role = context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "SinRol";
        var statusCode = context.Response.StatusCode;

        logger.LogInformation(
            "[AUDIT_LOG] User: {UserId} | Role: {Role} | {Method} {Path} | Status: {StatusCode} | Duration: {Elapsed}ms | IP: {IpAddress}",
            userId, role, method, path, statusCode, sw.ElapsedMilliseconds, ipAddress
        );
    }
}
