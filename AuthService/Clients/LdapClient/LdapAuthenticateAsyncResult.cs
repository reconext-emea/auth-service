namespace AuthService.Clients.LdapClient;

public record LdapAuthenticateAsyncResult(
    bool Success,
    string? Error = null,
    LdapUser? User = null
);
