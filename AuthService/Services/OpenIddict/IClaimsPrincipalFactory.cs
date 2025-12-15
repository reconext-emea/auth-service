using System.Security.Claims;
using AuthService.Models;

namespace AuthService.Services.OpenIddict;

public interface IClaimsPrincipalFactory
{
    Task<ClaimsPrincipal> Create(AuthServiceUser user, IEnumerable<string> scopes);
}
