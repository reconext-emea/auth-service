using AuthService.Clients.LdapClient;
using AuthService.Constants;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services.OpenIddict;

public class PasswordGrantHandler(
    ILdapClient ldap,
    UserManager<AuthServiceUser> userManager,
    IClaimsPrincipalFactory claimsFactory,
    OfficeLocationToRegionAdapter adapter
) : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly ILdapClient _ldap = ldap;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
    private readonly IClaimsPrincipalFactory _claimsFactory = claimsFactory;

    private readonly OfficeLocationToRegionAdapter _adapter = adapter;

    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
            return;

        string? username = context.Request.Username;
        string? password = context.Request.Password;

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

        var passport = new UserPassport(username, "reconext.com", password);
        LdapAuthenticateAsyncResult authResult = await _ldap.AuthenticateAsync(passport);

        if (!authResult.Success || authResult.User is null)
        {
            var (error, description) = TranslateLdapError(authResult.Error);
            context.Reject(error, description);
            return;
        }

        LdapUser ldapUser = authResult.User;

        string normalizedName = _userManager.NormalizeName(ldapUser.Username);
        AuthServiceUser? user = await _userManager
            .Users.Include(u => u.AppSettings)
            .Include(u => u.CustomProperties)
            .SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedName);

        if (user == null)
        {
            user = AuthServiceUser.CreateFromLdap(ldapUser, _adapter);
            IdentityResult result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                context.Reject(Errors.ServerError, "Failed to create user.");
                return;
            }
        }
        else
        {
            user.UpdateFromLdap(ldapUser, _adapter);
            await _userManager.UpdateAsync(user);
        }

        context.Principal = await _claimsFactory.Create(user, context.Request.GetScopes());
    }

    private static (string error, string description) TranslateLdapError(LdapError? ldapError)
    {
        return ldapError switch
        {
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
