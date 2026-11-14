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

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, applyMigrations: app.Environment.IsDevelopment());

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditLogMiddleware>();
app.MapControllers();

app.Run();
