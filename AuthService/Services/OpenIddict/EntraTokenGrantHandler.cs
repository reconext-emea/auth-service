using System.Security.Claims;
using AuthService.Clients.EntraIdClient;
using AuthService.Clients.GraphClient;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services.OpenIddict;

public class EntraTokenGrantHandler(
    IEntraIdClient entraId,
    UserManager<AuthServiceUser> userManager,
    IClaimsPrincipalFactory claimsFactory
) : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly IEntraIdClient _entraId = entraId;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
    private readonly IClaimsPrincipalFactory _claimsFactory = claimsFactory;

    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (
            !string.Equals(
                context.Request.GrantType,
                "urn:entra:access_token",
                StringComparison.Ordinal
            )
        )
            return;

        var entraAccessToken = (string?)context.Request.GetParameter("access_token");
        var entraGraphToken = (string?)context.Request.GetParameter("graph_token");

        EntraIdAuthenticateAsyncResult entraIdAuthenticateAsyncResult =
            await _entraId.AuthenticateAsync(entraAccessToken);

        if (!entraIdAuthenticateAsyncResult.Success)
        {
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{entraIdAuthenticateAsyncResult.ErrorDescription} ({entraIdAuthenticateAsyncResult.ErrorCode})"
            );
            return;
        }

        ClaimsPrincipal? principal = entraIdAuthenticateAsyncResult.Principal;

        if (principal == null)
        {
            var ex = new EntraIdException(EntraIdError.MissingPrincipal);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(entraGraphToken))
        {
            var ex = new EntraIdException(EntraIdError.MissingToken);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        var graphFactory = new Clients.GraphClient.GraphClientFactory();
        GraphServiceClient graph = graphFactory.InitializeFromAcquiredGraphToken(entraGraphToken);
        User? user;

        try
        {
            user = await graph.Me.GetAsync();
        }
        catch (Exception)
        {
            var ex = new EntraIdException(EntraIdError.GraphRequestFailed);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        if (user == null)
        {
            var ex = new EntraIdException(EntraIdError.MissingGraphUser);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        var graphOfficeLocation = user.OfficeLocation;

        if (string.IsNullOrWhiteSpace(graphOfficeLocation))
        {
            var ex = new EntraIdException(EntraIdError.MissingGraphUserOfficeLocation);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        var graphMail = user.Mail ?? user.UserPrincipalName;

        if (string.IsNullOrWhiteSpace(graphMail))
        {
            var ex = new EntraIdException(EntraIdError.MissingGraphUserMail);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        if (!graphMail.EndsWith("@reconext.com", StringComparison.OrdinalIgnoreCase))
        {
            var ex = new EntraIdException(EntraIdError.InvalidGraphUserMailFormat);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        var graphUsername = graphMail.Split('@')[0];

        if (string.IsNullOrWhiteSpace(graphUsername))
        {
            var ex = new EntraIdException(EntraIdError.InvalidGraphUserMailFormat);
            context.Reject(
                error: Errors.InvalidGrant,
                description: $"{ex.Description} ({ex.Error})"
            );
            return;
        }

        string normalizedName = _userManager.NormalizeName(graphUsername);

        AuthServiceUser? authServiceUser = await _userManager.Users.SingleOrDefaultAsync(u =>
            u.NormalizedUserName == normalizedName
        );

        var graphUser = new GraphUser(graphUsername, graphMail, graphOfficeLocation);

        if (authServiceUser == null)
        {
            authServiceUser = AuthServiceUser.CreateFromGraph(graphUser);
            IdentityResult identityResult = await _userManager.CreateAsync(authServiceUser);

            if (!identityResult.Succeeded)
            {
                var ex = new EntraIdException(EntraIdError.UserCreationFailed);
                context.Reject(
                    error: Errors.ServerError,
                    description: $"{ex.Description} ({ex.Error})"
                );
                return;
            }
        }
        else
        {
            authServiceUser.UpdateFromGraph(graphUser);
            await _userManager.UpdateAsync(authServiceUser);
        }

        context.Principal = _claimsFactory.Create(authServiceUser, context.Request.GetScopes());
    }
}
