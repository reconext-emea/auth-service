using System.Security.Claims;
using AuthService.Clients.LdapClient;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services.OpenIddict;

public class PasswordGrantHandler(LdapClient ldap, UserManager<AuthServiceUser> userManager)
    : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly LdapClient _ldap = ldap;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;

    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
            return;

        // Extract values
        string? username = context.Request.Username;
        string? password = context.Request.Password;
        string? domain = context.Request.GetParameter("domain")?.ToString();

        // ---- Input validation ----
        if (string.IsNullOrWhiteSpace(username))
        {
            context.Reject(Errors.InvalidRequest, "Username is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            context.Reject(Errors.InvalidRequest, "Password is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            context.Reject(Errors.InvalidRequest, "Domain is required.");
            return;
        }

        var passport = new UserPassport(username, domain, password);
        LdapAuthenticateAsyncResult authResult = await _ldap.AuthenticateAsync(passport);

        if (!authResult.Success || authResult.User is null)
        {
            var (error, description) = TranslateLdapError(authResult.Error);
            context.Reject(error, description);
            return;
        }

        LdapUser ldapUser = authResult.User;

        string normalizedName = _userManager.NormalizeName(ldapUser.Username);
        AuthServiceUser? user = await _userManager.Users.SingleOrDefaultAsync(u =>
            u.NormalizedUserName == normalizedName
        );

        if (user == null)
        {
            user = AuthServiceUser.CreateFromLdap(ldapUser);
            IdentityResult result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                context.Reject(Errors.ServerError, "Failed to create user.");
                return;
            }
        }
        else
        {
            user.UpdateFromLdap(ldapUser);
            await _userManager.UpdateAsync(user);
        }

        var identity = new ClaimsIdentity("Bearer");

        identity.AddClaim(
            new Claim("sub", user.Id).SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_username", user.UserName ?? "").SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_email", user.Email ?? "").SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_office_location", user.OfficeLocation ?? "").SetDestinations(
                Destinations.AccessToken,
                Destinations.IdentityToken
            )
        );

        identity.AddClaim(
            new Claim("sub_display_username", user.DisplayName ?? "").SetDestinations(
                Destinations.IdentityToken
            )
        );

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(context.Request.GetScopes());

        context.Principal = principal;
    }

    private static (string error, string description) TranslateLdapError(LdapError? ldapError)
    {
        return ldapError switch
        {
            LdapError.DomainNotAllowed => (
                Errors.InvalidRequest,
                "The specified domain is not allowed."
            ),
            LdapError.UserNotFound => (Errors.InvalidGrant, "User not found in LDAP directory."),
            LdapError.OfficeNotAllowed => (
                Errors.InvalidGrant,
                "Your office location is not authorized to access the system."
            ),
            LdapError.InvalidCredentials => (Errors.InvalidGrant, "Invalid username or password."),
            _ => (Errors.ServerError, "Unexpected LDAP error occurred."),
        };
    }
}
