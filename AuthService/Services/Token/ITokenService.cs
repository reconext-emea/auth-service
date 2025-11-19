using AuthService.Models;

namespace AuthService.Services.Token;

public interface ITokenService
{
    public Task<Microsoft.AspNetCore.Mvc.SignInResult> CreateTokenAsync(AuthServiceUser user);
}
