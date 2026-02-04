using System.Security.Claims;
using AuthService.Clients.EntraIdClient;
using AuthService.Clients.GraphClient;
using AuthService.Constants;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthService.Services.OpenIddict;

public class EntraTokenGrantHandler(
    IEntraIdClient entraId,
    UserManager<AuthServiceUser> userManager,
    IClaimsPrincipalFactory claimsFactory,
    OfficeLocationToRegionAdapter adapter
) : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly IEntraIdClient _entraId = entraId;
    private readonly UserManager<AuthServiceUser> _userManager = userManager;
    private readonly IClaimsPrincipalFactory _claimsFactory = claimsFactory;
    private readonly OfficeLocationToRegionAdapter _adapter = adapter;

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
        Microsoft.Graph.Models.User? user;

        try
        {
            user = await graph.Me.GetAsync(rc =>
            {
                rc.QueryParameters.Select =
                [
                    "id",
                    "displayName",
                    "userPrincipalName",
                    "mail",
                    "department",
                    "jobTitle",
                    "officeLocation",
                    "employeeId",
                ];
            });
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

        var graphAttributes = new GraphAttributes(
            EmployeeId: user.EmployeeId ?? string.Empty,
            DisplayName: user.DisplayName ?? string.Empty,
            Department: user.Department ?? string.Empty,
            JobTitle: user.JobTitle ?? string.Empty,
            OfficeLocation: user.OfficeLocation ?? string.Empty
        );

        if (string.IsNullOrWhiteSpace(graphAttributes.OfficeLocation))
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

        AuthServiceUser? authServiceUser = await _userManager
            .Users.Include(u => u.AppSettings)
            .Include(u => u.CustomProperties)
            .SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedName);

        var graphUser = new GraphUser(graphUsername, graphMail, graphAttributes);

        if (authServiceUser == null)
        {
            authServiceUser = AuthServiceUser.CreateFromGraph(graphUser, _adapter);
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
            authServiceUser.UpdateFromGraph(graphUser, _adapter);
            await _userManager.UpdateAsync(authServiceUser);
        }

        context.Principal = await _claimsFactory.Create(
            authServiceUser,
            context.Request.GetScopes()
        );
    }
}
