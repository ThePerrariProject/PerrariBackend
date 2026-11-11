using Perrari.Api.Extensions;
using Perrari.Api.Middlewares;
using Perrari.Api.Services;
using Perrari.Application;
using Perrari.Application.Interfaces.Services;
using Perrari.Infrastructure;
using Perrari.Infrastructure.Persistence.Seed;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthRateLimiting();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, applyMigrations: app.Environment.IsDevelopment());

// 1. Manejo global de excepciones (envuelve todo el pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Correlation ID para rastreo distribuido de peticiones
app.UseMiddleware<CorrelationIdMiddleware>();

// 3. Cabeceras de seguridad HTTP
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// 4. CORS (debe ir antes de RateLimiter, Authentication y Authorization)
app.UseCors("FrontendPolicy");

// 5. Limitador de peticiones
app.UseRateLimiter();

// 6. Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// 7. Auditoría de operaciones de escritura (después de auth para conocer al usuario y con tiempo de ejecución)
app.UseMiddleware<AuditLogMiddleware>();

// 8. Enrutamiento de controladores
app.MapControllers();

app.Run();

public partial class Program { }

