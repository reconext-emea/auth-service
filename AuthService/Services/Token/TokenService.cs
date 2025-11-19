using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;

namespace AuthService.Services.Token;

public class TokenService(SignInManager<AuthServiceUser> signInManager) : ITokenService
{
    private readonly SignInManager<AuthServiceUser> _signInManager = signInManager;

    public async Task<Microsoft.AspNetCore.Mvc.SignInResult> CreateTokenAsync(AuthServiceUser user)
    {
        // Create a ClaimsPrincipal for this user
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // Define the scopes permitted for this user session
        principal.SetScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            "api",
            OpenIddictConstants.Scopes.OfflineAccess
        );

        // Set token destinations for each claim
        foreach (var claim in principal.Claims)
        {
            // By default allow claim to go in access token
            claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);

            // If claim is email or name, include in ID token as well
            if (
                claim.Type == OpenIddictConstants.Claims.Email
                || claim.Type == OpenIddictConstants.Claims.Name
            )
            {
                claim.SetDestinations(
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken
                );
            }
        }

        // Return a SignInResult telling OpenIddict to issue the tokens
        return new Microsoft.AspNetCore.Mvc.SignInResult(principal);
    }
}
