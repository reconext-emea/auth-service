using System.Security.Claims;
using AuthService.Models;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services.OpenIddict;

public class ClaimsPrincipalFactory : IClaimsPrincipalFactory
{
    public ClaimsPrincipal Create(AuthServiceUser user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity("Bearer");

        identity.AddClaim(
            new Claim("sub", user.Id).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_username", user.UserName ?? string.Empty).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_email", user.Email ?? string.Empty).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_office_location", user.OfficeLocation).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_display_username", user.DisplayName).SetDestinations(
                Destinations.IdentityToken
            )
        );

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);

        return principal;
    }
}
