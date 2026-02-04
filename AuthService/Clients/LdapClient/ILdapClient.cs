namespace AuthService.Clients.LdapClient;

public interface ILdapClient
{
    Task<LdapAuthenticateAsyncResult> AuthenticateAsync(UserPassport passport);
    Task<LdapUser?> FindUserByUsernameAsync(string username, string domain, CancellationToken ct);
}
