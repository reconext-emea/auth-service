namespace AuthService.Clients.EntraIdClient;

public interface IEntraIdClient
{
    public Task<EntraIdAuthenticateAsyncResult> AuthenticateAsync(string? accessToken);
}
