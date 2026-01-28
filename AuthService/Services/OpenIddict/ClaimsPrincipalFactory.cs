using System.Security.Claims;
using System.Text.Json;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services.OpenIddict;

public class ClaimsPrincipalFactory(
    UserManager<AuthServiceUser> userManager,
    RoleManager<IdentityRole> roleManager
) : IClaimsPrincipalFactory
{
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    public async Task<ClaimsPrincipal> Create(AuthServiceUser user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity("Bearer");

        identity.AddClaim(
            new Claim("sub", user.Id).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("username", user.UserName ?? string.Empty).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("email", user.Email ?? string.Empty).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("office_location", user.OfficeLocation).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("display_username", user.DisplayName).SetDestinations(
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim(
                "app_settings",
                JsonSerializer.Serialize(
                    new
                    {
                        user.AppSettings.PreferredLanguageCode,
                        user.AppSettings.PreferredColorThemeCode,
                    }
                )
            ).SetDestinations(Destinations.IdentityToken)
        );

        identity.AddClaim(
            new Claim("confidentiality", user.CustomProperties.Confidentiality).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("region", user.CustomProperties.Region).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("employeeId", user.EmployeeId).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("department", user.Department).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("jobTitle", user.JobTitle).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        // ---------- LOAD USER "role" ----------
        var roles = await _userManager.GetRolesAsync(user);

        foreach (var roleName in roles)
        {
            identity.AddClaim(
                new Claim(Claims.Role, roleName).SetDestinations(
                    Destinations.AccessToken,
                    Destinations.IdentityToken
                )
            );
        }

        // ---------- LOAD ROLE CLAIMS "permission" ----------
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                continue;

            var roleClaims = await _roleManager.GetClaimsAsync(role);

            foreach (var claim in roleClaims)
            {
                identity.AddClaim(
                    new Claim(claim.Type, claim.Value).SetDestinations(Destinations.AccessToken)
                );
            }
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);

        return principal;
    }
}
