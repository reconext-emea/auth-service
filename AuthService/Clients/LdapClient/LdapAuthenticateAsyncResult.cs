namespace AuthService.Clients.LdapClient;

public record LdapAuthenticateAsyncResult(
    bool Success,
    LdapError? Error = null,
    LdapUser? User = null
);
