using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Clients.EntraIdClient;

class EntraIdClient(EntraIdConfig config, ILogger<EntraIdClient> logger) : IEntraIdClient
{
    private readonly EntraIdConfig _config = config;
    private readonly ILogger<EntraIdClient> _logger = logger;

    public async Task<EntraIdAuthenticateAsyncResult> AuthenticateAsync(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            var ex = new EntraIdException(EntraIdError.MissingToken);
            return new EntraIdAuthenticateAsyncResult(false, ex.Error, ex.Description);
        }

        try
        {
            ClaimsPrincipal principal = await ValidateToken(accessToken);

            return new EntraIdAuthenticateAsyncResult(true, null, null, principal);
        }
        catch (EntraIdException ex)
        {
            return new EntraIdAuthenticateAsyncResult(false, ex.Error, ex.Description);
        }
    }

    private async Task<ClaimsPrincipal> ValidateToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();

        var parameters = await GetTokenValidationParameters();

        ClaimsPrincipal principal;

        try
        {
            principal = handler.ValidateToken(accessToken, parameters, out var validated);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Reason}", ex.Message);
            throw new EntraIdException(EntraIdError.InvalidToken);
        }

        string? scope =
            principal.FindFirst("scp")?.Value
            ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;

        if (!string.Equals(scope, "access_as_user", StringComparison.OrdinalIgnoreCase))
            throw new EntraIdException(EntraIdError.NotAccessToken);

        return principal;
    }

    private async Task<TokenValidationParameters> GetTokenValidationParameters()
    {
        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{_config.IssuerV2}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever() { RequireHttps = false }
        );

        var config = await configManager.GetConfigurationAsync();

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = config.Issuer,
            ValidateIssuer = true,

            ValidAudience = _config.ClientId,
            ValidateAudience = true,

            IssuerSigningKeys = config.SigningKeys,
            ValidateIssuerSigningKey = true,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        };

        return validationParameters;
    }
}
