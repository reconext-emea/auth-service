namespace AuthService.Clients.LdapClient;

public interface ILdapClient
{
    Task<LdapAuthenticateAsyncResult> AuthenticateAsync(UserPassport passport);
}
