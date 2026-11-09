using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Perrari.Application.Common;

namespace Perrari.Api.Extensions;

public class HasPermissionAttribute(string permission) : AuthorizeAttribute($"{PolicyPrefix}{permission}")
{
    public const string PolicyPrefix = "perm:";
}

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(AuthClaims.Permission, permission)
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}
