using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Perrari.Application.Interfaces.Services;
using Perrari.Application.Services;
using Perrari.Application.Validators.Auth;

namespace Perrari.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IClientService, ClientService>();

        return services;
    }
}
